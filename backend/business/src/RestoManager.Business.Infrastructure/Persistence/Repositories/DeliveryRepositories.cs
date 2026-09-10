using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Delivery;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class DeliveryDriverRepository(BusinessDbContext db) : IDeliveryDriverRepository
{
    public Task<DeliveryDriver?> GetAsync(int id, CancellationToken ct = default)
        => db.DeliveryDrivers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.DeliveryDrivers.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<DeliveryDriver>> ListAsync(
        string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(search).OrderBy(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
        => Filter(search).CountAsync(ct);

    public void Add(DeliveryDriver driver) => db.DeliveryDrivers.Add(driver);

    private IQueryable<DeliveryDriver> Filter(string? search)
    {
        var q = db.DeliveryDrivers.AsQueryable();
        return string.IsNullOrWhiteSpace(search)
            ? q
            : q.Where(x => EF.Functions.ILike(x.LicensePlate, $"%{search.Trim()}%"));
    }
}

public sealed class DeliveryRepository(BusinessDbContext db) : IDeliveryRepository
{
    public Task<Delivery?> GetAsync(int id, CancellationToken ct = default)
        => db.Deliveries.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<(Delivery Delivery, Order Order)?> GetWithOrderAsync(int id, CancellationToken ct = default)
    {
        var pair = await (from d in db.Deliveries
                          join o in db.Orders on d.OrderId equals o.Id
                          where d.Id == id
                          select new { d, o }).FirstOrDefaultAsync(ct);
        return pair is null ? null : (pair.d, pair.o);
    }

    public Task<bool> ExistsForOrderAsync(int orderId, CancellationToken ct = default)
        => db.Deliveries.AnyAsync(x => x.OrderId == orderId, ct);

    public async Task<IReadOnlyList<(Delivery Delivery, Order Order)>> ListForBranchAsync(
        int branchId, DeliveryStatus? status, CancellationToken ct = default)
    {
        var q = from d in db.Deliveries
                join o in db.Orders on d.OrderId equals o.Id
                where o.BranchId == branchId
                select new { d, o };
        if (status is { } s)
        {
            q = q.Where(x => x.d.Status == s);
        }

        var rows = await q.OrderBy(x => x.d.EstimatedTime).ThenBy(x => x.d.Id).ToListAsync(ct);
        return rows.Select(x => (x.d, x.o)).ToList();
    }

    public void Add(Delivery delivery) => db.Deliveries.Add(delivery);
}
