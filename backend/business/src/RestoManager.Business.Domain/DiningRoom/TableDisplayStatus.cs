namespace RestoManager.Business.Domain.DiningRoom;

/// <summary>
/// Estado de uso <b>visible</b> de una mesa. Derivado por la aplicación (Anexo B),
/// nunca almacenado (TBL-01). <c>OUT_OF_SERVICE</c> y <c>CLEANING</c> reflejan el
/// estado operativo; <c>OCCUPIED</c>/<c>RESERVED</c>/<c>AVAILABLE</c> son puro cálculo.
/// </summary>
public enum TableDisplayStatus
{
    OutOfService,
    Cleaning,
    Occupied,
    Reserved,
    Available,
}

public static class TableDisplayStatusExtensions
{
    public static string ToDbValue(this TableDisplayStatus status) => status switch
    {
        TableDisplayStatus.OutOfService => "OUT_OF_SERVICE",
        TableDisplayStatus.Cleaning => "CLEANING",
        TableDisplayStatus.Occupied => "OCCUPIED",
        TableDisplayStatus.Reserved => "RESERVED",
        TableDisplayStatus.Available => "AVAILABLE",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

/// <summary>
/// Anexo B — regla de precedencia para visualizar una mesa. Función pura:
/// <c>OUT_OF_SERVICE → CLEANING → sesión abierta ⇒ OCCUPIED → reserva confirmada en
/// ventana ⇒ RESERVED → AVAILABLE</c>.
/// </summary>
public static class TableStatusResolver
{
    public static TableDisplayStatus Resolve(
        TableOperationalStatus operationalStatus, bool hasOpenSession, bool hasReservationInWindow) =>
        operationalStatus switch
        {
            TableOperationalStatus.OutOfService => TableDisplayStatus.OutOfService,
            TableOperationalStatus.Cleaning => TableDisplayStatus.Cleaning,
            _ when hasOpenSession => TableDisplayStatus.Occupied,
            _ when hasReservationInWindow => TableDisplayStatus.Reserved,
            _ => TableDisplayStatus.Available,
        };
}
