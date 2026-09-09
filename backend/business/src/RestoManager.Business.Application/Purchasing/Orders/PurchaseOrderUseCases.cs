using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Purchasing;

namespace RestoManager.Business.Application.Purchasing.Orders;

public sealed record PurchaseOrderLineDto(int IngredientId, decimal Quantity, decimal UnitPrice);

public sealed record PurchaseOrderDto(
    int Id, int SupplierId, int BranchId, DateOnly OrderDate, decimal TotalAmount, string Status,
    IReadOnlyList<PurchaseOrderLineDto> Items);

// ---- Crear (DRAFT) ----
public sealed record CreatePurchaseOrderCommand(
    int SupplierId, int BranchId, DateOnly OrderDate, IReadOnlyList<PurchaseOrderLineDto> Items);

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(line =>
        {
            line.RuleFor(l => l.IngredientId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreatePurchaseOrderHandler(
    IPurchaseOrderRepository purchaseOrders,
    ISupplierRepository suppliers,
    IIngredientRepository ingredients,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access,
    IValidator<CreatePurchaseOrderCommand> validator)
{
    public async Task<int> HandleAsync(CreatePurchaseOrderCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        access.EnsureCanOperate(command.BranchId);

        if (await suppliers.GetAsync(command.SupplierId, cancellationToken) is null)
        {
            throw new NotFoundException("proveedor", command.SupplierId);
        }
        foreach (var line in command.Items)
        {
            if (!await ingredients.ExistsAsync(line.IngredientId, cancellationToken))
            {
                throw new NotFoundException("ingrediente", line.IngredientId);
            }
        }

        var order = PurchaseOrder.Draft(
            command.SupplierId, command.BranchId, command.OrderDate,
            command.Items.Select(l => (l.IngredientId, l.Quantity, l.UnitPrice)));

        purchaseOrders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}

// ---- Transiciones ----
public sealed class SendPurchaseOrderHandler(
    IPurchaseOrderRepository purchaseOrders, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await purchaseOrders.GetAsync(id, cancellationToken) ?? throw new NotFoundException("orden de compra", id);
        access.EnsureCanOperate(order.BranchId);
        order.Send();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class CancelPurchaseOrderHandler(
    IPurchaseOrderRepository purchaseOrders, IUnitOfWork unitOfWork, BranchAccessGuard access)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await purchaseOrders.GetAsync(id, cancellationToken) ?? throw new NotFoundException("orden de compra", id);
        access.EnsureCanOperate(order.BranchId);
        order.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// §11.4 / INV-07: recibir la orden (solo total en Fase 3). Postea un movimiento
/// PURCHASE por cada ítem y pone la orden en RECEIVED, todo en una transacción.
/// Idempotente por (PURCHASE_ORDER, id, PURCHASE, ingrediente) — DOM-07.
/// </summary>
public sealed class ReceivePurchaseOrderHandler(
    IPurchaseOrderRepository purchaseOrders,
    InventoryLedger ledger,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    BranchAccessGuard access)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await purchaseOrders.GetAsync(id, cancellationToken) ?? throw new NotFoundException("orden de compra", id);
        access.EnsureCanOperate(order.BranchId);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            order.MarkReceived();
            foreach (var item in order.Items)
            {
                await ledger.PostAsync(
                    new LedgerEntry(
                        order.BranchId,
                        item.IngredientId,
                        MovementType.Purchase,
                        item.Quantity,
                        MovementReference.To(MovementReferenceTypes.PurchaseOrder, order.Id),
                        currentUser.EmployeeId),
                    token);
            }
        }, cancellationToken);
    }
}

// ---- Consultas ----
public sealed record ListPurchaseOrdersQuery(int? BranchId, string? Status, int Page = 1, int PageSize = 20);

public sealed class ListPurchaseOrdersHandler(IPurchaseOrderRepository purchaseOrders)
{
    public async Task<PagedResult<PurchaseOrderDto>> HandleAsync(ListPurchaseOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        PurchaseOrderStatus? status = query.Status is null ? null : PurchaseOrderStatusExtensions.FromDbValue(query.Status);
        var items = await purchaseOrders.ListAsync(query.BranchId, status, page.Skip, page.Take, cancellationToken);
        var total = await purchaseOrders.CountAsync(query.BranchId, status, cancellationToken);
        return new PagedResult<PurchaseOrderDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static PurchaseOrderDto Map(PurchaseOrder o) => new(
        o.Id, o.SupplierId, o.BranchId, o.OrderDate, o.TotalAmount, o.Status.ToDbValue(),
        o.Items.Select(i => new PurchaseOrderLineDto(i.IngredientId, i.Quantity, i.UnitPrice)).ToList());
}

public sealed class GetPurchaseOrderHandler(IPurchaseOrderRepository purchaseOrders)
{
    public async Task<PurchaseOrderDto> HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var o = await purchaseOrders.GetAsync(id, cancellationToken) ?? throw new NotFoundException("orden de compra", id);
        return ListPurchaseOrdersHandler.Map(o);
    }
}
