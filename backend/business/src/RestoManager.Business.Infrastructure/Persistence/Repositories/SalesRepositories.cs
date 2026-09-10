using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(BusinessDbContext db) : IOrderRepository
{
    public Task<Order?> GetAsync(int id, CancellationToken ct = default)
        => WithChildren(db.Orders).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Order>> ListAsync(
        int branchId,
        OrderChannel? channel,
        OrderStatus? status,
        int? tableSessionId,
        DateTime? fromInclusive,
        DateTime? toExclusive,
        int skip,
        int take,
        CancellationToken ct = default)
        => await WithChildren(Filter(branchId, channel, status, tableSessionId, fromInclusive, toExclusive))
            .OrderByDescending(x => x.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountAsync(
        int branchId,
        OrderChannel? channel,
        OrderStatus? status,
        int? tableSessionId,
        DateTime? fromInclusive,
        DateTime? toExclusive,
        CancellationToken ct = default)
        => Filter(branchId, channel, status, tableSessionId, fromInclusive, toExclusive).CountAsync(ct);

    public async Task<IReadOnlyList<Order>> ListForCustomerAsync(
        int customerId, int skip, int take, CancellationToken ct = default)
        => await WithChildren(db.Orders.Where(x => x.CustomerId == customerId))
            .OrderByDescending(x => x.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountForCustomerAsync(int customerId, CancellationToken ct = default)
        => db.Orders.CountAsync(x => x.CustomerId == customerId, ct);

    public void Add(Order order) => db.Orders.Add(order);

    private static IQueryable<Order> WithChildren(IQueryable<Order> q) =>
        q.Include(x => x.Items).Include(x => x.Discounts).Include(x => x.Payments);

    private IQueryable<Order> Filter(
        int branchId,
        OrderChannel? channel,
        OrderStatus? status,
        int? tableSessionId,
        DateTime? fromInclusive,
        DateTime? toExclusive)
    {
        var q = db.Orders.Where(x => x.BranchId == branchId);
        if (channel is { } c)
        {
            q = q.Where(x => x.Channel == c);
        }
        if (status is { } s)
        {
            q = q.Where(x => x.Status == s);
        }
        if (tableSessionId is { } sid)
        {
            q = q.Where(x => x.TableSessionId == sid);
        }
        if (fromInclusive is { } from)
        {
            q = q.Where(x => x.OrderTime >= from);
        }
        if (toExclusive is { } to)
        {
            q = q.Where(x => x.OrderTime < to);
        }
        return q;
    }
}

public sealed class DiscountRepository(BusinessDbContext db) : IDiscountRepository
{
    public Task<Discount?> GetAsync(int id, CancellationToken ct = default)
        => db.Discounts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Discount?> GetByNameAsync(string name, CancellationToken ct = default)
        => db.Discounts.FirstOrDefaultAsync(x => x.Name == name, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.Discounts.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Discount>> ListAsync(
        string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(search).OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
        => Filter(search).CountAsync(ct);

    public void Add(Discount discount) => db.Discounts.Add(discount);

    private IQueryable<Discount> Filter(string? search)
    {
        var q = db.Discounts.AsQueryable();
        return string.IsNullOrWhiteSpace(search)
            ? q
            : q.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim()}%"));
    }
}
