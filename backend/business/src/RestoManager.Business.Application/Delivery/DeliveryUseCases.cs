using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Sales.Consumption;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Delivery;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Application.Deliveries;

public sealed record DeliveryDto(
    int Id, int OrderId, int DriverId, string DeliveryAddress,
    DateTime EstimatedTime, DateTime? ActualTime, string Status,
    string OrderChannel, string OrderStatus, DateTime OrderTime, decimal OrderTotal, int? CustomerId);

internal static class DeliveryMapper
{
    public static DeliveryDto Map(Delivery d, Order o) => new(
        d.Id, d.OrderId, d.DriverId, d.DeliveryAddress, d.EstimatedTime, d.ActualTime, d.Status.ToDbValue(),
        o.Channel.ToDbValue(), o.Status.ToDbValue(), o.OrderTime, o.TotalAmount, o.CustomerId);
}

// ─────────────────────────── Consultas (sucursal activa) ───────────────────────────

public sealed record ListDeliveriesQuery(string? Status);

public sealed class ListDeliveriesHandler(IDeliveryRepository deliveries, IBranchContext branchContext)
{
    public async Task<IReadOnlyList<DeliveryDto>> HandleAsync(ListDeliveriesQuery query, CancellationToken ct = default)
    {
        DeliveryStatus? status = null;
        if (query.Status is not null)
        {
            if (!DeliveryStatusExtensions.TryFromDbValue(query.Status, out var s))
            {
                throw new DomainRuleException("delivery.invalid_status", $"Estado '{query.Status}' inválido.");
            }
            status = s;
        }

        var rows = await deliveries.ListForBranchAsync(branchContext.BranchId, status, ct);
        return rows.Select(r => DeliveryMapper.Map(r.Delivery, r.Order)).ToList();
    }
}

public sealed class GetDeliveryHandler(IDeliveryRepository deliveries)
{
    public async Task<DeliveryDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var row = await deliveries.GetWithOrderAsync(id, ct) ?? throw new NotFoundException("entrega", id);
        return DeliveryMapper.Map(row.Delivery, row.Order);
    }
}

// ─────────────────────────── Transiciones ───────────────────────────

public sealed record AssignDriverCommand(int DeliveryId, int DriverId);

public sealed class AssignDriverHandler(
    IDeliveryRepository deliveries,
    IDeliveryDriverRepository drivers,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access)
{
    public async Task HandleAsync(AssignDriverCommand command, CancellationToken ct = default)
    {
        var row = await deliveries.GetWithOrderAsync(command.DeliveryId, ct)
            ?? throw new NotFoundException("entrega", command.DeliveryId);
        access.EnsureCanOperate(row.Order.BranchId);

        if (!await drivers.ExistsAsync(command.DriverId, ct))
        {
            throw new NotFoundException("repartidor", command.DriverId);
        }

        row.Delivery.AssignDriver(command.DriverId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>Transiciones simples de estado: en camino, entregada, fallida.</summary>
public sealed class AdvanceDeliveryHandler(
    IDeliveryRepository deliveries, IUnitOfWork unitOfWork, BranchAccessGuard access, IClock clock)
{
    public enum Step { InTransit, Delivered, Failed }

    public async Task HandleAsync(int deliveryId, Step step, CancellationToken ct = default)
    {
        var row = await deliveries.GetWithOrderAsync(deliveryId, ct)
            ?? throw new NotFoundException("entrega", deliveryId);
        access.EnsureCanOperate(row.Order.BranchId);

        var delivery = row.Delivery;
        switch (step)
        {
            case Step.InTransit: delivery.MarkInTransit(); break;
            case Step.Delivered: delivery.MarkDelivered(clock.UtcNow); break;
            case Step.Failed: delivery.MarkFailed(clock.UtcNow); break;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Cancela la entrega y, en la misma transacción, cancela el pedido (decisión Fase 7):
/// pagos <c>CONFIRMED</c> → <c>REFUNDED</c> y reversa del stock si estaba <c>PAID</c>.
/// Se rechaza si el pedido ya está <c>CLOSED</c>.
/// </summary>
public sealed class CancelDeliveryHandler(
    IDeliveryRepository deliveries,
    IOrderRepository orders,
    SaleConsumptionService saleConsumption,
    IUnitOfWork unitOfWork,
    BranchAccessGuard access)
{
    public async Task HandleAsync(int deliveryId, CancellationToken ct = default)
    {
        var row = await deliveries.GetWithOrderAsync(deliveryId, ct)
            ?? throw new NotFoundException("entrega", deliveryId);
        access.EnsureCanOperate(row.Order.BranchId);

        var delivery = row.Delivery;
        if (delivery.Status == DeliveryStatus.Cancelled)
        {
            return;
        }

        var order = await orders.GetAsync(delivery.OrderId, ct)
            ?? throw new NotFoundException("pedido", delivery.OrderId);
        if (order.Status == OrderStatus.Closed)
        {
            throw new DomainRuleException(
                "delivery.order_closed", "No se puede cancelar la entrega de un pedido ya cerrado.");
        }

        var orderWasPaid = order.Status == OrderStatus.Paid;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            delivery.Cancel();
            order.CancelOrder();
            if (orderWasPaid)
            {
                await saleConsumption.ReverseForOrderAsync(order.Id, token);
            }
        }, ct);
    }
}
