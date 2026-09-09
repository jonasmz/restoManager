using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Inventory;

namespace RestoManager.Business.Application.Inventory.Stock;

public sealed record BranchStockLineDto(
    int IngredientId, string IngredientName, string Unit, decimal StockQuantity, decimal UnitPrice, decimal StockValue);

public sealed record MovementDto(
    int Id, int IngredientId, string MovementType, decimal Quantity, DateTime MovementTime,
    string? ReferenceType, int? ReferenceId, int? EmployeeId);

// ---- Stock actual por sucursal (spec §12.2) ----
public sealed class GetBranchStockHandler(
    IBranchInventoryRepository balances,
    IIngredientRepository ingredients,
    IBranchContext branchContext)
{
    public async Task<IReadOnlyList<BranchStockLineDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var rows = await balances.ListByBranchAsync(branchContext.BranchId, cancellationToken);
        var result = new List<BranchStockLineDto>(rows.Count);
        foreach (var row in rows)
        {
            var ingredient = await ingredients.GetAsync(row.IngredientId, cancellationToken);
            result.Add(new BranchStockLineDto(
                row.IngredientId,
                ingredient?.Name ?? string.Empty,
                ingredient?.Unit ?? string.Empty,
                row.StockQuantity,
                ingredient?.UnitPrice ?? 0m,
                row.StockQuantity * (ingredient?.UnitPrice ?? 0m)));
        }
        return result;
    }
}

// ---- Historial de movimientos (spec §12.3) ----
public sealed record ListMovementsQuery(int? IngredientId, DateTime? From, DateTime? To, string? MovementType);

public sealed class ListMovementsHandler(IInventoryMovementRepository movements, IBranchContext branchContext)
{
    public async Task<IReadOnlyList<MovementDto>> HandleAsync(ListMovementsQuery query, CancellationToken cancellationToken = default)
    {
        MovementType? type = query.MovementType is null
            ? null
            : MovementTypeExtensions.FromDbValue(query.MovementType);

        var rows = await movements.ListAsync(branchContext.BranchId, query.IngredientId, query.From, query.To, type, cancellationToken);
        return rows.Select(m => new MovementDto(
            m.Id, m.IngredientId, m.MovementType.ToDbValue(), m.Quantity, m.MovementTime,
            m.ReferenceType, m.ReferenceId, m.EmployeeId)).ToList();
    }
}
