namespace RestoManager.Business.Domain.Inventory;

/// <summary>
/// Registro de merma. Crear el log ES la confirmación (decisión de Fase 3): en la
/// misma transacción se postea el movimiento WASTE equivalente (INV-08).
/// </summary>
public sealed class WasteLog
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime LoggedTime { get; private set; }
    public int LoggedBy { get; private set; }

    private WasteLog() { }

    public static WasteLog Create(
        int branchId, int ingredientId, decimal quantity, string reason, DateTime loggedTime, int loggedBy)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad de merma debe ser mayor que cero.");
        }

        return new WasteLog
        {
            BranchId = branchId,
            IngredientId = ingredientId,
            Quantity = quantity,
            Reason = string.IsNullOrWhiteSpace(reason)
                ? throw new ArgumentException("El motivo es obligatorio.", nameof(reason))
                : reason.Trim(),
            LoggedTime = loggedTime,
            LoggedBy = loggedBy,
        };
    }
}
