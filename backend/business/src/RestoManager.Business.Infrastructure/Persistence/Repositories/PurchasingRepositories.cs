using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Purchasing;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class SupplierRepository(BusinessDbContext db) : ISupplierRepository
{
    public Task<Supplier?> GetAsync(int id, CancellationToken cancellationToken = default)
        => db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        string? search, int skip, int take, CancellationToken cancellationToken = default)
        => await Filter(search).OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken = default)
        => Filter(search).CountAsync(cancellationToken);

    public void Add(Supplier supplier) => db.Suppliers.Add(supplier);

    private IQueryable<Supplier> Filter(string? search)
    {
        var q = db.Suppliers.AsQueryable();
        return string.IsNullOrWhiteSpace(search)
            ? q
            : q.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim()}%"));
    }
}

public sealed class PurchaseOrderRepository(BusinessDbContext db) : IPurchaseOrderRepository
{
    public Task<PurchaseOrder?> GetAsync(int id, CancellationToken cancellationToken = default)
        => db.PurchaseOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PurchaseOrder>> ListAsync(
        int? branchId, PurchaseOrderStatus? status, int skip, int take, CancellationToken cancellationToken = default)
        => await Filter(branchId, status)
            .Include(x => x.Items)
            .OrderByDescending(x => x.Id)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(
        int? branchId, PurchaseOrderStatus? status, CancellationToken cancellationToken = default)
        => Filter(branchId, status).CountAsync(cancellationToken);

    public void Add(PurchaseOrder order) => db.PurchaseOrders.Add(order);

    private IQueryable<PurchaseOrder> Filter(int? branchId, PurchaseOrderStatus? status)
    {
        var q = db.PurchaseOrders.AsQueryable();
        if (branchId is { } b)
        {
            q = q.Where(x => x.BranchId == b);
        }
        if (status is { } s)
        {
            q = q.Where(x => x.Status == s);
        }
        return q;
    }
}
