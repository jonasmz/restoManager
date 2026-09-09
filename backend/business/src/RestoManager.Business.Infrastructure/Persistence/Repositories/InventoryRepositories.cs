using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Inventory;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class IngredientRepository(BusinessDbContext db) : IIngredientRepository
{
    public Task<Ingredient?> GetAsync(int id, CancellationToken cancellationToken = default)
        => db.Ingredients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        => db.Ingredients.AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ingredient>> ListAsync(
        string? search, int skip, int take, CancellationToken cancellationToken = default)
        => await Filter(search).OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken = default)
        => Filter(search).CountAsync(cancellationToken);

    public void Add(Ingredient ingredient) => db.Ingredients.Add(ingredient);

    private IQueryable<Ingredient> Filter(string? search)
    {
        var q = db.Ingredients.AsQueryable();
        return string.IsNullOrWhiteSpace(search)
            ? q
            : q.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim()}%"));
    }
}

public sealed class BranchInventoryRepository(BusinessDbContext db) : IBranchInventoryRepository
{
    public Task<BranchInventory?> GetAsync(int branchId, int ingredientId, CancellationToken cancellationToken = default)
        => db.BranchInventories.FirstOrDefaultAsync(
            x => x.BranchId == branchId && x.IngredientId == ingredientId, cancellationToken);

    public async Task<IReadOnlyList<BranchInventory>> ListByBranchAsync(int branchId, CancellationToken cancellationToken = default)
        => await db.BranchInventories.Where(x => x.BranchId == branchId)
            .OrderBy(x => x.IngredientId).ToListAsync(cancellationToken);

    public void Add(BranchInventory balance) => db.BranchInventories.Add(balance);
}

public sealed class InventoryMovementRepository(BusinessDbContext db) : IInventoryMovementRepository
{
    public void Add(InventoryMovement movement) => db.InventoryMovements.Add(movement);

    public Task<bool> ExistsForReferenceAsync(
        string referenceType, int referenceId, MovementType movementType, int ingredientId,
        CancellationToken cancellationToken = default)
        => db.InventoryMovements.AnyAsync(
            m => m.ReferenceType == referenceType
                 && m.ReferenceId == referenceId
                 && m.MovementType == movementType
                 && m.IngredientId == ingredientId,
            cancellationToken);

    public async Task<IReadOnlyList<InventoryMovement>> ListAsync(
        int branchId, int? ingredientId, DateTime? from, DateTime? to, MovementType? movementType,
        CancellationToken cancellationToken = default)
    {
        var q = db.InventoryMovements.Where(m => m.BranchId == branchId);
        if (ingredientId is { } ing)
        {
            q = q.Where(m => m.IngredientId == ing);
        }
        if (from is { } f)
        {
            q = q.Where(m => m.MovementTime >= f);
        }
        if (to is { } t)
        {
            q = q.Where(m => m.MovementTime <= t);
        }
        if (movementType is { } mt)
        {
            q = q.Where(m => m.MovementType == mt);
        }

        return await q.OrderBy(m => m.MovementTime).ThenBy(m => m.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovement>> ListByReferenceAsync(
        string referenceType, int referenceId, CancellationToken cancellationToken = default)
        => await db.InventoryMovements
            .Where(m => m.ReferenceType == referenceType && m.ReferenceId == referenceId)
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);
}

public sealed class WasteLogRepository(BusinessDbContext db) : IWasteLogRepository
{
    public void Add(WasteLog wasteLog) => db.WasteLogs.Add(wasteLog);

    public async Task<IReadOnlyList<WasteLog>> ListAsync(
        int branchId, int? ingredientId, CancellationToken cancellationToken = default)
    {
        var q = db.WasteLogs.Where(w => w.BranchId == branchId);
        if (ingredientId is { } ing)
        {
            q = q.Where(w => w.IngredientId == ing);
        }
        return await q.OrderByDescending(w => w.LoggedTime).ThenByDescending(w => w.Id).ToListAsync(cancellationToken);
    }
}
