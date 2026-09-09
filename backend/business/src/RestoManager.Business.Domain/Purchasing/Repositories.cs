namespace RestoManager.Business.Domain.Purchasing;

public interface ISupplierRepository
{
    Task<Supplier?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> ListAsync(string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, CancellationToken cancellationToken = default);
    void Add(Supplier supplier);
}

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseOrder>> ListAsync(
        int? branchId, PurchaseOrderStatus? status, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(int? branchId, PurchaseOrderStatus? status, CancellationToken cancellationToken = default);
    void Add(PurchaseOrder order);
}
