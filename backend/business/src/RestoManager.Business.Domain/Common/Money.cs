namespace RestoManager.Business.Domain.Common;

/// <summary>
/// Redondeo monetario del sistema (decisión Fase 6): medio-arriba
/// (<see cref="MidpointRounding.AwayFromZero"/>), 2 decimales. Se aplica al total de
/// cada línea y al total del pedido.
/// </summary>
public static class Money
{
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
