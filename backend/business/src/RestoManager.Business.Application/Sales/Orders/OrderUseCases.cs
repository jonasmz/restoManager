using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Sales.Consumption;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Application.Sales.Orders;

public sealed record OrderItemDto(
    int Id, int MenuItemId, int Quantity, decimal UnitPrice, decimal LineTotal, string? Notes);

public sealed record OrderDiscountDto(int Id, int DiscountId, decimal AppliedAmount);

public sealed record PaymentDto(
    int Id, string PaymentMethod, decimal Amount, DateTime PaymentTime, string Status);

public sealed record OrderDto(
    int Id, int BranchId, string Channel, string Status, int? TableId, int? TableSessionId,
    int? CustomerId, int EmployeeId, DateTime OrderTime,
    decimal ItemsSubtotal, decimal DiscountTotal, decimal TotalAmount, decimal ConfirmedPaid, decimal Balance,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderDiscountDto> Discounts,
    IReadOnlyList<PaymentDto> Payments);

// ─────────────────────────── Crear pedido ───────────────────────────

/// <summary>
/// Abre un pedido en la sucursal activa con canal explícito. El empleado es el del
/// token. MESA exige <paramref name="TableId"/>; los demás canales lo rechazan
/// (junto con <paramref name="TableSessionId"/>).
/// </summary>
public sealed record CreateOrderCommand(string Channel, int? TableId, int? TableSessionId, int? CustomerId);

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Channel)
            .Must(c => OrderChannelExtensions.TryFromDbValue(c, out _))
            .WithMessage("Canal inválido. Use MESA, BARRA, TAKEAWAY o DELIVERY.");
        RuleFor(x => x.TableId).GreaterThan(0).When(x => x.TableId is not null);
        RuleFor(x => x.TableSessionId).GreaterThan(0).When(x => x.TableSessionId is not null);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null);

        // ORD-06: solo el canal MESA admite mesa y sesión.
        RuleFor(x => x.TableId)
            .Null().When(x => !IsMesa(x.Channel), ApplyConditionTo.CurrentValidator)
            .WithMessage("Solo un pedido de canal MESA puede indicar una mesa.");
        RuleFor(x => x.TableSessionId)
            .Null().When(x => !IsMesa(x.Channel), ApplyConditionTo.CurrentValidator)
            .WithMessage("Solo un pedido de canal MESA puede indicar una sesión de mesa.");
    }

    private static bool IsMesa(string? channel) =>
        OrderChannelExtensions.TryFromDbValue(channel, out var c) && c == OrderChannel.Mesa;
}

public sealed class CreateOrderHandler(
    IOrderRepository orders,
    ITableRepository tables,
    ITableSessionRepository sessions,
    IEmployeeRepository employees,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IBranchContext branchContext,
    ICurrentUser currentUser,
    BranchAccessGuard access,
    IClock clock,
    IValidator<CreateOrderCommand> validator)
{
    public async Task<int> HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var branchId = branchContext.BranchId;
        access.EnsureCanOperate(branchId);

        var channel = OrderChannelExtensions.FromDbValue(command.Channel);

        // DOM-01 / ORD-04: el pedido vive en la sucursal del empleado del token.
        var employee = await employees.GetAsync(currentUser.EmployeeId, ct)
            ?? throw new NotFoundException("empleado", currentUser.EmployeeId);
        if (employee.BranchId != branchId)
        {
            throw new DomainRuleException(
                "sales.employee_branch_mismatch",
                "El empleado del token no pertenece a la sucursal activa.");
        }

        int? tableId = null;
        int? tableSessionId = null;

        if (channel == OrderChannel.Mesa)
        {
            if (command.TableId is not { } requestedTableId)
            {
                throw new DomainRuleException(
                    "sales.table_required", "Un pedido de canal MESA requiere una mesa.");
            }

            var table = await tables.GetAsync(requestedTableId, ct)
                ?? throw new NotFoundException("mesa", requestedTableId);

            // DOM-01 / ORD-04: la mesa es de la sucursal activa.
            if (table.BranchId != branchId)
            {
                throw new DomainRuleException(
                    "sales.order_branch_mismatch", "La mesa no pertenece a la sucursal activa.");
            }
            tableId = table.Id;

            if (command.TableSessionId is { } sid)
            {
                var session = await sessions.GetAsync(sid, ct)
                    ?? throw new NotFoundException("sesión de mesa", sid);

                // DOM-02 / ORD-03: la sesión es de esa misma mesa y está abierta.
                if (session.TableId != table.Id)
                {
                    throw new DomainRuleException(
                        "sales.session_table_mismatch", "La sesión no corresponde a la mesa indicada.");
                }
                if (!session.IsOpen)
                {
                    throw new DomainRuleException(
                        "sales.session_closed", "La sesión de mesa está cerrada.");
                }
                tableSessionId = session.Id;
            }
        }

        if (command.CustomerId is { } customerId && !await customers.ExistsAsync(customerId, ct))
        {
            throw new NotFoundException("cliente", customerId);
        }

        var order = Order.Create(
            channel, branchId, currentUser.EmployeeId, clock.UtcNow, tableId, tableSessionId, command.CustomerId);
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(ct);
        return order.Id;
    }
}

// ─────────────────────────── Ítems del pedido ───────────────────────────

public sealed record AddOrderItemCommand(int OrderId, int MenuItemId, int Quantity, string? Notes);

public sealed class AddOrderItemValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemValidator()
    {
        RuleFor(x => x.MenuItemId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(255);
    }
}

public sealed class AddOrderItemHandler(
    IOrderRepository orders,
    IMenuItemRepository menuItems,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IValidator<AddOrderItemCommand> validator)
{
    public async Task<int> HandleAsync(AddOrderItemCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        var menuItem = await menuItems.GetAsync(command.MenuItemId, ct)
            ?? throw new NotFoundException("plato", command.MenuItemId);

        var item = order.AddItem(command.MenuItemId, command.Quantity, menuItem.Price, command.Notes);
        await unitOfWork.SaveChangesAsync(ct);
        return item.Id;
    }
}

public sealed record UpdateOrderItemCommand(int OrderId, int ItemId, int Quantity, string? Notes);

public sealed class UpdateOrderItemValidator : AbstractValidator<UpdateOrderItemCommand>
{
    public UpdateOrderItemValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(255);
    }
}

public sealed class UpdateOrderItemHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IValidator<UpdateOrderItemCommand> validator)
{
    public async Task HandleAsync(UpdateOrderItemCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        order.UpdateItem(command.ItemId, command.Quantity, command.Notes);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed record RemoveOrderItemCommand(int OrderId, int ItemId);

public sealed class RemoveOrderItemHandler(
    IOrderRepository orders, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(RemoveOrderItemCommand command, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        order.RemoveItem(command.ItemId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

// ─────────────────────────── Consultas (sucursal activa) ───────────────────────────

public sealed record ListOrdersQuery(
    string? Channel, string? Status, int? SessionId, DateOnly? Date, int Page = 1, int PageSize = 20);

public sealed class ListOrdersHandler(IOrderRepository orders, IBranchContext branchContext)
{
    public async Task<PagedResult<OrderDto>> HandleAsync(ListOrdersQuery query, CancellationToken ct = default)
    {
        OrderChannel? channel = null;
        if (query.Channel is not null)
        {
            if (!OrderChannelExtensions.TryFromDbValue(query.Channel, out var c))
            {
                throw new DomainRuleException("sales.invalid_channel", $"Canal '{query.Channel}' inválido.");
            }
            channel = c;
        }

        OrderStatus? status = null;
        if (query.Status is not null)
        {
            if (!OrderStatusExtensions.TryFromDbValue(query.Status, out var s))
            {
                throw new DomainRuleException("sales.invalid_status", $"Estado '{query.Status}' inválido.");
            }
            status = s;
        }

        DateTime? from = query.Date is { } d
            ? DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified)
            : null;
        DateTime? to = from?.AddDays(1);

        var page = new PageRequest(query.Page, query.PageSize);
        var items = await orders.ListAsync(
            branchContext.BranchId, channel, status, query.SessionId, from, to, page.Skip, page.Take, ct);
        var total = await orders.CountAsync(
            branchContext.BranchId, channel, status, query.SessionId, from, to, ct);

        return new PagedResult<OrderDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static OrderDto Map(Order o) => new(
        o.Id, o.BranchId, o.Channel.ToDbValue(), o.Status.ToDbValue(),
        o.TableId, o.TableSessionId, o.CustomerId, o.EmployeeId, o.OrderTime,
        o.ItemsSubtotal, o.DiscountTotal, o.TotalAmount, o.ConfirmedPaid, o.Balance,
        o.Items.Select(i => new OrderItemDto(i.Id, i.MenuItemId, i.Quantity, i.UnitPrice, i.LineTotal, i.Notes))
            .ToList(),
        o.Discounts.Select(d => new OrderDiscountDto(d.Id, d.DiscountId, d.AppliedAmount)).ToList(),
        o.Payments
            .Select(p => new PaymentDto(
                p.Id, p.PaymentMethod.ToDbValue(), p.Amount, p.PaymentTime, p.Status.ToDbValue()))
            .ToList());
}

public sealed class GetOrderHandler(IOrderRepository orders)
{
    public async Task<OrderDto> HandleAsync(int id, CancellationToken ct = default)
        => ListOrdersHandler.Map(await orders.GetAsync(id, ct) ?? throw new NotFoundException("pedido", id));
}

// ─────────────────────────── Descuentos del pedido (Fase 6b) ───────────────────────────

public sealed record ApplyOrderDiscountCommand(int OrderId, int DiscountId);

public sealed class ApplyOrderDiscountHandler(
    IOrderRepository orders,
    IDiscountRepository discounts,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IClock clock)
{
    public async Task<int> HandleAsync(ApplyOrderDiscountCommand command, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        var discount = await discounts.GetAsync(command.DiscountId, ct)
            ?? throw new NotFoundException("descuento", command.DiscountId);

        var row = order.ApplyDiscount(discount, DateOnly.FromDateTime(clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return row.Id;
    }
}

public sealed record RemoveOrderDiscountCommand(int OrderId, int DiscountId);

public sealed class RemoveOrderDiscountHandler(
    IOrderRepository orders, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(RemoveOrderDiscountCommand command, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        order.RemoveDiscount(command.DiscountId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

// ─────────────────────────── Pagos (Fase 6b) ───────────────────────────

public sealed record RegisterPaymentCommand(int OrderId, string Method, decimal Amount, int? GiftCardId);

public sealed class RegisterPaymentValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentValidator()
    {
        RuleFor(x => x.Method)
            .Must(m => PaymentMethodExtensions.TryFromDbValue(m, out _))
            .WithMessage("Medio de pago inválido. Use CASH, CARD, TRANSFER, GIFT_CARD u OTHER.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.GiftCardId)
            .NotNull().When(x => x.Method == "GIFT_CARD")
            .WithMessage("Un pago con tarjeta regalo requiere la tarjeta.");
        RuleFor(x => x.GiftCardId)
            .Null().When(x => x.Method != "GIFT_CARD")
            .WithMessage("Solo un pago GIFT_CARD indica tarjeta regalo.");
    }
}

public sealed class RegisterPaymentHandler(
    IOrderRepository orders,
    IGiftCardRepository giftCards,
    SaleConsumptionService saleConsumption,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IClock clock,
    IValidator<RegisterPaymentCommand> validator)
{
    public async Task<int> HandleAsync(RegisterPaymentCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        var order = await orders.GetAsync(command.OrderId, ct)
            ?? throw new NotFoundException("pedido", command.OrderId);
        access.EnsureCanOperate(order.BranchId);

        var method = PaymentMethodExtensions.FromDbValue(command.Method);
        var now = clock.UtcNow;

        GiftCard? card = null;
        if (method == PaymentMethod.GiftCard)
        {
            card = await giftCards.GetAsync(command.GiftCardId!.Value, ct)
                ?? throw new NotFoundException("tarjeta regalo", command.GiftCardId!.Value);
        }

        var statusBefore = order.Status;
        Payment payment = null!;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            payment = order.RegisterPayment(method, command.Amount, now);
            if (card is not null)
            {
                giftCards.AddTransaction(card.Redeem(order.Id, command.Amount, now));
            }

            // Evento de dominio del SALE (decisión Fase 6): el 1.er pago CONFIRMED pasa
            // el pedido de OPEN a PAID y dispara el descuento de stock por receta (§7.5).
            if (statusBefore == OrderStatus.Open && order.Status == OrderStatus.Paid)
            {
                await saleConsumption.PostForOrderAsync(order, token);
            }
        }, ct);

        return payment.Id;
    }
}

// ─────────────────────────── Cierre y cancelación (Fase 6b) ───────────────────────────

public sealed class CloseOrderHandler(IOrderRepository orders, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(orderId, ct) ?? throw new NotFoundException("pedido", orderId);
        access.EnsureCanOperate(order.BranchId);

        order.CloseOrder();
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class CancelOrderHandler(
    IOrderRepository orders,
    SaleConsumptionService saleConsumption,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access)
{
    public async Task HandleAsync(int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(orderId, ct) ?? throw new NotFoundException("pedido", orderId);
        access.EnsureCanOperate(order.BranchId);

        var statusBefore = order.Status;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            order.CancelOrder();

            // Si el pedido ya estaba contabilizado (PAID), revertir el consumo de stock
            // en la misma transacción (nunca se borran movimientos: se postea el opuesto).
            if (statusBefore == OrderStatus.Paid)
            {
                await saleConsumption.ReverseForOrderAsync(order.Id, token);
            }
        }, ct);
    }
}
