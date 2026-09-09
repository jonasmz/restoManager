namespace RestoManager.Business.Domain.Sales;

public interface IOrderRepository
{
    /// <summary>Carga el pedido con sus ítems, descuentos y pagos.</summary>
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

public interface IDiscountRepository
{
    Task<Discount?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Discount>> ListAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    void Add(Discount discount);
}
