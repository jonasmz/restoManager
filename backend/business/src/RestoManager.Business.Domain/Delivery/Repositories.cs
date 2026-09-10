using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Domain.Delivery;

public interface IDeliveryDriverRepository
{
    Task<DeliveryDriver?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryDriver>> ListAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    void Add(DeliveryDriver driver);
}

public interface IDeliveryRepository
{
    Task<Delivery?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Entrega + su pedido (para resolver sucursal, total y estado del pedido).</summary>
    Task<(Delivery Delivery, Order Order)?> GetWithOrderAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsForOrderAsync(int orderId, CancellationToken ct = default);

    /// <summary>Entregas de la sucursal (join con <c>orders</c>), opcionalmente por estado.</summary>
    Task<IReadOnlyList<(Delivery Delivery, Order Order)>> ListForBranchAsync(
        int branchId, DeliveryStatus? status, CancellationToken ct = default);

    void Add(Delivery delivery);
}
