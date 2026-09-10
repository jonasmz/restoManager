using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.DiningRoom;

// Módulo Salón (Fase 5). Ciclo de sesiones (SES-*), estado de uso derivado por la
// aplicación (Anexo B, TBL-01) y reservas (RSV-*). El estado de uso
// AVAILABLE/RESERVED/OCCUPIED nunca se persiste.

// ─────────────────────────── Mesa ───────────────────────────

/// <summary>Operatividad física de la mesa. Fijo por CHECK en la BD.</summary>
public enum TableOperationalStatus
{
    Active,
    Cleaning,
    OutOfService,
}

public static class TableOperationalStatusExtensions
{
    public static string ToDbValue(this TableOperationalStatus status) => status switch
    {
        TableOperationalStatus.Active => "ACTIVE",
        TableOperationalStatus.Cleaning => "CLEANING",
        TableOperationalStatus.OutOfService => "OUT_OF_SERVICE",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static TableOperationalStatus FromDbValue(string value) => value switch
    {
        "ACTIVE" => TableOperationalStatus.Active,
        "CLEANING" => TableOperationalStatus.Cleaning,
        "OUT_OF_SERVICE" => TableOperationalStatus.OutOfService,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "tables.operational_status inválido"),
    };
}

/// <summary>Mesa de salón. La barra NO es una fila de esta tabla (RSV-04, estructural).</summary>
public sealed class Table
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int Number { get; private set; }
    public int Capacity { get; private set; }
    public TableOperationalStatus OperationalStatus { get; private set; }

    private Table() { }

    public Table(int branchId, int number, int capacity)
    {
        BranchId = branchId;
        OperationalStatus = TableOperationalStatus.Active;
        UpdateDetails(number, capacity);
    }

    public void UpdateDetails(int number, int capacity)
    {
        if (number <= 0)
        {
            throw new DomainRuleException("dining.invalid_table_number", "El número de mesa debe ser mayor que cero.");
        }
        if (capacity <= 0)
        {
            throw new DomainRuleException("dining.invalid_capacity", "La capacidad de la mesa debe ser mayor que cero.");
        }
        Number = number;
        Capacity = capacity;
    }

    /// <summary>
    /// Cambio de operatividad. No se puede pasar a <c>CLEANING</c>/<c>OUT_OF_SERVICE</c>
    /// con una sesión abierta (coherencia con Anexo B / DOM-03).
    /// </summary>
    public void SetOperationalStatus(TableOperationalStatus next, bool hasOpenSession)
    {
        if (next != TableOperationalStatus.Active && hasOpenSession)
        {
            throw new DomainRuleException(
                "dining.table_busy",
                "No se puede cambiar el estado operativo de una mesa con una sesión abierta.");
        }
        OperationalStatus = next;
    }
}

// ─────────────────────── Sesión de mesa ───────────────────────

/// <summary>
/// Ocupación real de una mesa (§4.3). Una sesión abierta tiene <c>ClosedAt = null</c>;
/// la BD respalda "una abierta por mesa" con un índice único parcial (SES-01).
/// </summary>
public sealed class TableSession
{
    public int Id { get; private set; }
    public int TableId { get; private set; }
    public int GuestCount { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public bool IsOpen => ClosedAt is null;

    private TableSession() { }

    /// <summary>
    /// SES-04: <c>guest_count &gt; 0</c>. SES-05: <c>guest_count &lt;= tableCapacity</c>
    /// (la sesión no puede tener más comensales que la capacidad de la mesa).
    /// </summary>
    public static TableSession Open(int tableId, int guestCount, int tableCapacity, DateTime now)
    {
        if (guestCount <= 0)
        {
            throw new DomainRuleException("dining.invalid_guest_count", "El número de comensales debe ser mayor que cero.");
        }
        if (guestCount > tableCapacity)
        {
            throw new DomainRuleException(
                "dining.guest_count_exceeds_capacity",
                $"La mesa admite hasta {tableCapacity} comensales.");
        }
        return new TableSession { TableId = tableId, GuestCount = guestCount, OpenedAt = now };
    }

    /// <summary>SES-03: <c>closed_at &gt;= opened_at</c>; fija <c>closed_at = now</c>.</summary>
    public void Close(DateTime now)
    {
        if (!IsOpen)
        {
            throw new DomainRuleException("dining.session_already_closed", "La sesión ya está cerrada.");
        }
        if (now < OpenedAt)
        {
            throw new DomainRuleException("dining.close_before_open", "El cierre no puede ser anterior a la apertura.");
        }
        ClosedAt = now;
    }
}

// ───────────────────────── Reserva ─────────────────────────

/// <summary>Ciclo de la reserva (catálogo confirmado en transversal §2).</summary>
public enum ReservationStatus
{
    Pending,
    Confirmed,
    Seated,
    Cancelled,
    NoShow,
}

public static class ReservationStatusExtensions
{
    public static string ToDbValue(this ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "PENDING",
        ReservationStatus.Confirmed => "CONFIRMED",
        ReservationStatus.Seated => "SEATED",
        ReservationStatus.Cancelled => "CANCELLED",
        ReservationStatus.NoShow => "NO_SHOW",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static ReservationStatus FromDbValue(string value) => value switch
    {
        "PENDING" => ReservationStatus.Pending,
        "CONFIRMED" => ReservationStatus.Confirmed,
        "SEATED" => ReservationStatus.Seated,
        "CANCELLED" => ReservationStatus.Cancelled,
        "NO_SHOW" => ReservationStatus.NoShow,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "reservations.status inválido"),
    };
}

/// <summary>
/// Reserva de una mesa. RSV-02: no crea por sí misma una <c>TableSession</c>.
/// RSV-03: <c>RESERVED</c> es derivado (ver <see cref="MakesTableReservedAt"/>).
/// </summary>
public sealed class Reservation
{
    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public int BranchId { get; private set; }
    public int TableId { get; private set; }
    public DateTime ReservationTime { get; private set; }
    public int PartySize { get; private set; }
    public ReservationStatus Status { get; private set; }

    private Reservation() { }

    public static Reservation Create(int customerId, int branchId, int tableId, DateTime reservationTime, int partySize)
    {
        if (partySize <= 0)
        {
            throw new DomainRuleException("dining.invalid_party_size", "El número de comensales debe ser mayor que cero.");
        }
        return new Reservation
        {
            CustomerId = customerId,
            BranchId = branchId,
            TableId = tableId,
            ReservationTime = reservationTime,
            PartySize = partySize,
            Status = ReservationStatus.Pending,
        };
    }

    public void Confirm()
    {
        Require(ReservationStatus.Pending, "confirmar");
        Status = ReservationStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status is ReservationStatus.Seated or ReservationStatus.Cancelled or ReservationStatus.NoShow)
        {
            throw new DomainRuleException(
                "dining.reservation_invalid_transition",
                $"No se puede cancelar una reserva en estado {Status.ToDbValue()}.");
        }
        Status = ReservationStatus.Cancelled;
    }

    public void MarkNoShow()
    {
        Require(ReservationStatus.Confirmed, "marcar como no-show");
        Status = ReservationStatus.NoShow;
    }

    /// <summary>Se marca al abrir la sesión desde la reserva (flujo §11.3).</summary>
    public void MarkSeated()
    {
        Require(ReservationStatus.Confirmed, "sentar");
        Status = ReservationStatus.Seated;
    }

    /// <summary>
    /// RSV-03 / TBL-02: ¿esta reserva pone la mesa en <c>RESERVED</c> en el instante
    /// <paramref name="now"/>? Solo <c>CONFIRMED</c> dentro de la ventana configurada.
    /// </summary>
    public bool MakesTableReservedAt(DateTime now, ReservationWindow window) =>
        Status == ReservationStatus.Confirmed
        && now >= ReservationTime - window.Before
        && now <= ReservationTime + window.After;

    private void Require(ReservationStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainRuleException(
                "dining.reservation_invalid_transition",
                $"No se puede {action} una reserva en estado {Status.ToDbValue()}.");
        }
    }
}
