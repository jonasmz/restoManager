using RestoManager.Business.Domain.Common;

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

    /// <summary>
    /// Punto de reposición (Fase 9): un insumo está "bajo stock" en una sucursal
    /// cuando <c>ReorderPoint &gt; 0</c> y el saldo de <see cref="BranchInventory"/>
    /// es menor o igual. <c>0</c> = sin control (nunca marca bajo stock).
    /// </summary>
    public decimal ReorderPoint { get; private set; }

    /// <summary>Fecha de baja lógica (issue #47). <c>null</c> = ingrediente activo.</summary>
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Ingredient() { }

    public Ingredient(string name, string unit, decimal unitPrice, decimal reorderPoint = 0m)
    {
        Rename(name);
        SetUnit(unit);
        SetUnitPrice(unitPrice);
        SetReorderPoint(reorderPoint);
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

    public void SetReorderPoint(decimal reorderPoint)
    {
        if (reorderPoint < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reorderPoint), "El punto de reposición no puede ser negativo.");
        }
        ReorderPoint = reorderPoint;
    }

    /// <summary>
    /// Baja lógica (issue #47): no elimina la fila, solo la oculta de altas/listados/
    /// edición. Las recetas/movimientos/compras que ya lo referencian no se ven afectados.
    /// </summary>
    public void Delete(DateTime deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("inventory.ingredient_already_deleted", "El ingrediente ya fue eliminado.");
        }
        DeletedAt = deletedAt;
    }
}

internal static class Guard
{
    public static string NotBlank(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"'{name}' es obligatorio.", name)
            : value.Trim();
}
