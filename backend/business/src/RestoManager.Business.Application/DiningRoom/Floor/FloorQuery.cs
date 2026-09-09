using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.DiningRoom;

namespace RestoManager.Business.Application.DiningRoom.Floor;

/// <summary>Una mesa del tablero con su estado de uso derivado (Anexo B).</summary>
public sealed record FloorTableDto(
    int TableId,
    int Number,
    int Capacity,
    string OperationalStatus,
    string DisplayStatus,
    int? OpenSessionId,
    DateTime? OpenedAt,
    DateTime? NextReservationTime);

/// <summary>
/// Tablero de salón de la sucursal activa: estado derivado de <b>todas</b> las mesas
/// en <c>now</c>. Solo lectura (TBL-01).
/// </summary>
public sealed class GetFloorHandler(
    ITableRepository tables,
    ITableSessionRepository sessions,
    IReservationRepository reservations,
    IBranchContext branchContext,
    BranchAccessGuard access,
    ReservationWindow window,
    IClock clock)
{
    public async Task<IReadOnlyList<FloorTableDto>> HandleAsync(CancellationToken ct = default)
    {
        var branchId = branchContext.BranchId;
        access.EnsureCanOperate(branchId);

        var branchTables = await tables.ListByBranchAsync(branchId, ct);
        if (branchTables.Count == 0)
        {
            return [];
        }

        var now = clock.UtcNow;
        var tableIds = branchTables.Select(t => t.Id).ToArray();

        var openSessionTableIds = (await sessions.TableIdsWithOpenSessionAsync(tableIds, ct)).ToHashSet();
        var openSessions = new Dictionary<int, TableSession>();
        foreach (var id in openSessionTableIds)
        {
            if (await sessions.GetOpenForTableAsync(id, ct) is { } s)
            {
                openSessions[id] = s;
            }
        }

        var confirmed = await reservations.ConfirmedForTablesInRangeAsync(
            tableIds, now - window.After, now + window.Before, ct);
        var reservedNow = confirmed
            .Where(r => r.MakesTableReservedAt(now, window))
            .GroupBy(r => r.TableId)
            .ToDictionary(g => g.Key, g => g.Min(r => r.ReservationTime));

        return branchTables
            .OrderBy(t => t.Number)
            .Select(t =>
            {
                var hasOpenSession = openSessionTableIds.Contains(t.Id);
                var hasReservation = reservedNow.TryGetValue(t.Id, out var nextTime);
                var display = TableStatusResolver.Resolve(t.OperationalStatus, hasOpenSession, hasReservation);
                openSessions.TryGetValue(t.Id, out var session);
                return new FloorTableDto(
                    t.Id, t.Number, t.Capacity,
                    t.OperationalStatus.ToDbValue(),
                    display.ToDbValue(),
                    session?.Id,
                    session?.OpenedAt,
                    hasReservation ? nextTime : null);
            })
            .ToList();
    }
}
