using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Inventory;

/// <summary>
/// Saldo actual de un ingrediente en una sucursal (INV-02: uno por par
/// sucursal+ingrediente; INV-03: no negativo).
/// </summary>
public sealed class BranchInventory
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int IngredientId { get; private set; }
    public decimal StockQuantity { get; private set; }

    private BranchInventory() { }

    public static BranchInventory Start(int branchId, int ingredientId) => new()
    {
        BranchId = branchId,
        IngredientId = ingredientId,
        StockQuantity = 0m,
    };

    /// <summary>Aplica una variación firmada al saldo. Rechaza dejarlo negativo (INV-03).</summary>
    public void Apply(decimal delta)
    {
        var next = StockQuantity + delta;
        if (next < 0)
        {
            throw new DomainRuleException(
                "inventory.insufficient_stock",
                $"Stock insuficiente: saldo {StockQuantity}, variación {delta}.");
        }
        StockQuantity = next;
    }
}
