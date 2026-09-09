namespace RestoManager.Business.Domain.DiningRoom;

/// <summary>
/// TBL-02: ventana temporal en la que una reserva <c>CONFIRMED</c> transforma la mesa
/// en <c>RESERVED</c> (estado derivado, nunca persistido). El esquema no la fija; la
/// define la aplicación y es configurable (<c>Salon:ReservationWindow</c>).
/// </summary>
public sealed record ReservationWindow(TimeSpan Before, TimeSpan After)
{
    /// <summary>Por defecto: 30 min antes / 30 min después de <c>reservation_time</c>.</summary>
    public static ReservationWindow Default { get; } =
        new(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

    public static ReservationWindow FromMinutes(int beforeMinutes, int afterMinutes) => new(
        TimeSpan.FromMinutes(Math.Max(0, beforeMinutes)),
        TimeSpan.FromMinutes(Math.Max(0, afterMinutes)));
}
