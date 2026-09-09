namespace RestoManager.Business.Domain.Inventory;

public interface IIngredientRepository
{
    Task<Ingredient?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ingredient>> ListAsync(string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, CancellationToken cancellationToken = default);
    void Add(Ingredient ingredient);
}

public interface IBranchInventoryRepository
{
    Task<BranchInventory?> GetAsync(int branchId, int ingredientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchInventory>> ListByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    void Add(BranchInventory balance);
}

public interface IInventoryMovementRepository
{
    void Add(InventoryMovement movement);

    /// <summary>
    /// Idempotencia (DOM-07): ¿ya existe un movimiento de ese tipo, para esa referencia
    /// y ese ingrediente? (el ingrediente distingue los ítems de una misma orden de compra).
    /// </summary>
    Task<bool> ExistsForReferenceAsync(
        string referenceType,
        int referenceId,
        MovementType movementType,
        int ingredientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> ListAsync(
        int branchId,
        int? ingredientId,
        DateTime? from,
        DateTime? to,
        MovementType? movementType,
        CancellationToken cancellationToken = default);

    /// <summary>Todos los movimientos de una referencia (p. ej. <c>ORDER</c> + id de pedido).</summary>
    Task<IReadOnlyList<InventoryMovement>> ListByReferenceAsync(
        string referenceType, int referenceId, CancellationToken cancellationToken = default);
}

public interface IWasteLogRepository
{
    void Add(WasteLog wasteLog);
    Task<IReadOnlyList<WasteLog>> ListAsync(int branchId, int? ingredientId, CancellationToken cancellationToken = default);
}
