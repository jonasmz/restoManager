using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Inventory;

namespace RestoManager.Business.Application.Inventory.Ledger;

/// <summary>Una variación de stock a registrar en el libro.</summary>
public sealed record LedgerEntry(
    int BranchId,
    int IngredientId,
    MovementType MovementType,
    decimal SignedQuantity,
    MovementReference Reference,
    int? EmployeeId);

/// <summary>
/// Núcleo del inventario (spec §7): inserta el movimiento firmado y ajusta el saldo
/// de la sucursal de forma coherente. NO hace <c>SaveChanges</c>: el caso de uso lo
/// envuelve en una transacción (INV-05). Garantiza:
/// <list type="bullet">
///   <item>DOM-05 — el movimiento y el saldo son de la misma sucursal e ingrediente.</item>
///   <item>DOM-07 — idempotencia por (referencia, tipo, ingrediente).</item>
///   <item>INV-03 — el saldo nunca queda negativo.</item>
///   <item>INV-06 — cantidad distinta de cero, con el signo correcto por tipo.</item>
/// </list>
/// </summary>
public sealed class InventoryLedger(
    IBranchInventoryRepository balances,
    IInventoryMovementRepository movements,
    IClock clock)
{
    /// <returns><c>true</c> si posteó; <c>false</c> si ya existía (operación idempotente).</returns>
    public async Task<bool> PostAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Reference is { Type: { } type, Id: { } referenceId } &&
            await movements.ExistsForReferenceAsync(type, referenceId, entry.MovementType, entry.IngredientId, cancellationToken))
        {
            return false;
        }

        var movement = InventoryMovement.Create(
            entry.BranchId,
            entry.IngredientId,
            entry.MovementType,
            entry.SignedQuantity,
            clock.UtcNow,
            entry.Reference,
            entry.EmployeeId);

        var balance = await balances.GetAsync(entry.BranchId, entry.IngredientId, cancellationToken);
        if (balance is null)
        {
            balance = BranchInventory.Start(entry.BranchId, entry.IngredientId);
            balances.Add(balance);
        }

        balance.Apply(entry.SignedQuantity);
        movements.Add(movement);
        return true;
    }
}
