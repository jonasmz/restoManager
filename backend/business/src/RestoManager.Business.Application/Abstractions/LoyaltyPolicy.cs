namespace RestoManager.Business.Application.Abstractions;

/// <summary>
/// Política de fidelidad (Fase 8, decisión 1). La Infra la construye desde
/// configuración (<c>Loyalty:*</c>).
/// </summary>
/// <param name="PointsPerCurrencyUnit">
/// Puntos que acumula el cliente por cada unidad monetaria del total de un pedido
/// <c>CLOSED</c> (<c>floor(total) * PointsPerCurrencyUnit</c>). Por defecto 1.
/// </param>
/// <param name="RedeemRate">
/// Puntos necesarios para descontar una unidad monetaria al canjear
/// (<c>importe = puntos / RedeemRate</c>). Por defecto 100.
/// </param>
public sealed record LoyaltyPolicy(int PointsPerCurrencyUnit, decimal RedeemRate)
{
    /// <summary>Nombre de la fila de <c>discounts</c> de sistema que respalda el canje.</summary>
    public const string SystemDiscountName = "Canje de puntos de fidelidad";

    public static LoyaltyPolicy Default { get; } = new(PointsPerCurrencyUnit: 1, RedeemRate: 100m);

    public int PointsFor(decimal orderTotal) =>
        orderTotal <= 0m ? 0 : (int)Math.Floor(orderTotal) * PointsPerCurrencyUnit;

    public decimal AmountFor(int points) => points <= 0 ? 0m : points / RedeemRate;
}
