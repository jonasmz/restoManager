using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Inventory;

/// <summary>
/// Asiento del libro de inventario (spec §7.3). Cantidad firmada: PURCHASE &gt; 0,
/// SALE &lt; 0, WASTE &lt; 0, ADJUSTMENT ±. Nunca cero (INV-06).
/// </summary>
public sealed class InventoryMovement
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int IngredientId { get; private set; }
    public MovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime MovementTime { get; private set; }
    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public int? EmployeeId { get; private set; }

    private InventoryMovement() { }

    public static InventoryMovement Create(
        int branchId,
        int ingredientId,
        MovementType type,
        decimal signedQuantity,
        DateTime movementTime,
        MovementReference reference,
        int? employeeId)
    {
        if (signedQuantity == 0)
        {
            throw new DomainRuleException("inventory.zero_quantity", "La cantidad del movimiento no puede ser cero (INV-06).");
        }

        var expected = type.ExpectedSign();
        if (expected != 0 && Math.Sign(signedQuantity) != expected)
        {
            throw new DomainRuleException(
                "inventory.wrong_sign",
                $"El movimiento {type.ToDbValue()} requiere signo {(expected > 0 ? "positivo" : "negativo")}.");
        }

        return new InventoryMovement
        {
            BranchId = branchId,
            IngredientId = ingredientId,
            MovementType = type,
            Quantity = signedQuantity,
            MovementTime = movementTime,
            ReferenceType = reference.Type,
            ReferenceId = reference.Id,
            EmployeeId = employeeId,
        };
    }
}
