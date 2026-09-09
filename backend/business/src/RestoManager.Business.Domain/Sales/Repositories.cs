namespace RestoManager.Business.Domain.Sales;

public interface IOrderRepository
{
    /// <summary>Carga el pedido con sus ítems.</summary>
    Task<Order?> GetAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> ListAsync(
        int branchId,
        OrderChannel? channel,
        OrderStatus? status,
        int? tableSessionId,
        DateTime? fromInclusive,
        DateTime? toExclusive,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<int> CountAsync(
        int branchId,
        OrderChannel? channel,
        OrderStatus? status,
        int? tableSessionId,
        DateTime? fromInclusive,
        DateTime? toExclusive,
        CancellationToken ct = default);

    void Add(Order order);
}
