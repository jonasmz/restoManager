namespace RestoManager.Business.Domain.Inventory;

/// <summary>Tipo de movimiento de inventario (CHECK fijo en BD, spec §7.3).</summary>
public enum MovementType
{
    Purchase,
    Sale,
    Waste,
    Adjustment,
}

public static class MovementTypeExtensions
{
    public static string ToDbValue(this MovementType type) => type switch
    {
        MovementType.Purchase => "PURCHASE",
        MovementType.Sale => "SALE",
        MovementType.Waste => "WASTE",
        MovementType.Adjustment => "ADJUSTMENT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static MovementType FromDbValue(string value) => value switch
    {
        "PURCHASE" => MovementType.Purchase,
        "SALE" => MovementType.Sale,
        "WASTE" => MovementType.Waste,
        "ADJUSTMENT" => MovementType.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "movement_type inválido"),
    };

    /// <summary>Signo esperado de la cantidad: +1, -1 o 0 (ADJUSTMENT admite ambos).</summary>
    public static int ExpectedSign(this MovementType type) => type switch
    {
        MovementType.Purchase => 1,
        MovementType.Sale => -1,
        MovementType.Waste => -1,
        MovementType.Adjustment => 0,
        _ => 0,
    };
}
