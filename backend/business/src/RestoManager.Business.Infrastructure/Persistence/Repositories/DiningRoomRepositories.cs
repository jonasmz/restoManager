using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.DiningRoom;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class TableRepository(BusinessDbContext db) : ITableRepository
{
    public Task<Table?> GetAsync(int id, CancellationToken ct = default)
        => db.Tables.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Table>> ListByBranchAsync(int branchId, CancellationToken ct = default)
        => await db.Tables.Where(x => x.BranchId == branchId).OrderBy(x => x.Number).ToListAsync(ct);

    public Task<bool> NumberExistsAsync(int branchId, int number, int? excludeId, CancellationToken ct = default)
        => db.Tables.AnyAsync(
            x => x.BranchId == branchId && x.Number == number && (excludeId == null || x.Id != excludeId), ct);

    public void Add(Table table) => db.Tables.Add(table);
}

public sealed class TableSessionRepository(BusinessDbContext db) : ITableSessionRepository
{
    public Task<TableSession?> GetAsync(int id, CancellationToken ct = default)
        => db.TableSessions.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<TableSession?> GetOpenForTableAsync(int tableId, CancellationToken ct = default)
        => db.TableSessions.FirstOrDefaultAsync(x => x.TableId == tableId && x.ClosedAt == null, ct);

    public async Task<IReadOnlyList<int>> TableIdsWithOpenSessionAsync(
        IReadOnlyCollection<int> tableIds, CancellationToken ct = default)
        => await db.TableSessions
            .Where(x => x.ClosedAt == null && tableIds.Contains(x.TableId))
            .Select(x => x.TableId)
            .Distinct()
            .ToListAsync(ct);

    public void Add(TableSession session) => db.TableSessions.Add(session);
}

public sealed class ReservationRepository(BusinessDbContext db) : IReservationRepository
{
    public Task<Reservation?> GetAsync(int id, CancellationToken ct = default)
        => db.Reservations.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Reservation>> ListAsync(
        int branchId, DateTime? fromInclusive, DateTime? toExclusive, ReservationStatus? status,
        CancellationToken ct = default)
    {
        var q = db.Reservations.Where(x => x.BranchId == branchId);
        if (fromInclusive is { } f)
        {
            q = q.Where(x => x.ReservationTime >= f);
        }
        if (toExclusive is { } t)
        {
            q = q.Where(x => x.ReservationTime < t);
        }
        if (status is { } s)
        {
            q = q.Where(x => x.Status == s);
        }
        return await q.OrderBy(x => x.ReservationTime).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Reservation>> ConfirmedForTablesInRangeAsync(
        IReadOnlyCollection<int> tableIds, DateTime fromInclusive, DateTime toInclusive,
        CancellationToken ct = default)
        => await db.Reservations
            .Where(x => x.Status == ReservationStatus.Confirmed
                && tableIds.Contains(x.TableId)
                && x.ReservationTime >= fromInclusive
                && x.ReservationTime <= toInclusive)
            .ToListAsync(ct);

    public Task<bool> HasOverlappingAsync(
        int tableId, DateTime fromInclusive, DateTime toInclusive, int? excludeId, CancellationToken ct = default)
        => db.Reservations.AnyAsync(
            x => x.TableId == tableId
                && (excludeId == null || x.Id != excludeId)
                && (x.Status == ReservationStatus.Pending
                    || x.Status == ReservationStatus.Confirmed
                    || x.Status == ReservationStatus.Seated)
                && x.ReservationTime >= fromInclusive
                && x.ReservationTime <= toInclusive,
            ct);

    public void Add(Reservation reservation) => db.Reservations.Add(reservation);
}
