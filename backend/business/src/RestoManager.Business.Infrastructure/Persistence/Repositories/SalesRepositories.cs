using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(BusinessDbContext db) : IOrderRepository
{
    public Task<Order?> GetAsync(int id, CancellationToken ct = default)
        => db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);

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
        => await Filter(branchId, channel, status, tableSessionId, fromInclusive, toExclusive)
            .Include(x => x.Items)
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

    public void Add(Order order) => db.Orders.Add(order);

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
