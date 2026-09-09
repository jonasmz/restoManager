namespace RestoManager.Business.Domain.DiningRoom;

public interface ITableRepository
{
    Task<Table?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Table>> ListByBranchAsync(int branchId, CancellationToken ct = default);
    Task<bool> NumberExistsAsync(int branchId, int number, int? excludeId, CancellationToken ct = default);
    void Add(Table table);
}

public interface ITableSessionRepository
{
    Task<TableSession?> GetAsync(int id, CancellationToken ct = default);
    Task<TableSession?> GetOpenForTableAsync(int tableId, CancellationToken ct = default);

    /// <summary>Subconjunto de <paramref name="tableIds"/> que tienen una sesión abierta.</summary>
    Task<IReadOnlyList<int>> TableIdsWithOpenSessionAsync(
        IReadOnlyCollection<int> tableIds, CancellationToken ct = default);

    void Add(TableSession session);
}

public interface IReservationRepository
{
    Task<Reservation?> GetAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Reservation>> ListAsync(
        int branchId, DateTime? fromInclusive, DateTime? toExclusive, ReservationStatus? status,
        CancellationToken ct = default);

    /// <summary>
    /// Reservas <c>CONFIRMED</c> de esas mesas cuya <c>reservation_time</c> cae en
    /// <c>[fromInclusive, toExclusive]</c> — usado para derivar <c>RESERVED</c> en el tablero.
    /// </summary>
    Task<IReadOnlyList<Reservation>> ConfirmedForTablesInRangeAsync(
        IReadOnlyCollection<int> tableIds, DateTime fromInclusive, DateTime toInclusive,
        CancellationToken ct = default);

    /// <summary>¿Hay otra reserva activa (PENDING/CONFIRMED/SEATED) para la mesa en ese rango horario?</summary>
    Task<bool> HasOverlappingAsync(
        int tableId, DateTime fromInclusive, DateTime toInclusive, int? excludeId, CancellationToken ct = default);

    void Add(Reservation reservation);
}
