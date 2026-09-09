namespace RestoManager.Business.Domain.Inventory;

/// <summary>
/// Catálogo común de materias primas. NO tiene stock (INV-01): la existencia vive
/// en <see cref="BranchInventory"/> por sucursal.
/// </summary>
public sealed class Ingredient
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }

    private Ingredient() { }

    public Ingredient(string name, string unit, decimal unitPrice)
    {
        Rename(name);
        SetUnit(unit);
        SetUnitPrice(unitPrice);
    }

    public void Rename(string name)
    {
        Name = Guard.NotBlank(name, nameof(name));
    }

    public void SetUnit(string unit)
    {
        Unit = Guard.NotBlank(unit, nameof(unit));
    }

    public void SetUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
        }
        UnitPrice = unitPrice;
    }
}

internal static class Guard
{
    public static string NotBlank(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"'{name}' es obligatorio.", name)
            : value.Trim();
}
