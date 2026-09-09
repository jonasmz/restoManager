namespace RestoManager.Business.Domain.DiningRoom;

// Módulo Salón. Solo esquema en la Fase 3; la Fase 5 añade el ciclo de sesiones,
// el estado derivado (Anexo B) y las reservas.

public sealed class Table
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int Number { get; set; }
    public int Capacity { get; set; }

    /// <summary>ACTIVE | CLEANING | OUT_OF_SERVICE (CHECK en BD).</summary>
    public string OperationalStatus { get; set; } = "ACTIVE";
}

public sealed class TableSession
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public int GuestCount { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public sealed class Reservation
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int BranchId { get; set; }
    public int TableId { get; set; }
    public DateTime ReservationTime { get; set; }
    public int PartySize { get; set; }
    public string Status { get; set; } = string.Empty;
}
