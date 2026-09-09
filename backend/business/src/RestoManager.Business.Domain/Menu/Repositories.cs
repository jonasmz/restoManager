namespace RestoManager.Business.Domain.Menu;

public interface ICategoryRepository
{
    Task<Category?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> ListAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    void Add(Category category);
}

public interface IMenuItemRepository
{
    /// <summary>Carga el plato con su receta e impuestos.</summary>
    Task<MenuItem?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<MenuItem>> ListAsync(int? categoryId, string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(int? categoryId, string? search, CancellationToken ct = default);
    void Add(MenuItem menuItem);
}

public interface IKitchenStationRepository
{
    /// <summary>Carga la estación con sus platos asignados.</summary>
    Task<KitchenStation?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<KitchenStation>> ListByBranchAsync(int branchId, CancellationToken ct = default);
    void Add(KitchenStation station);
}

public interface IMenuItemAvailabilityRepository
{
    Task<MenuItemBranchAvailability?> GetAsync(int branchId, int menuItemId, CancellationToken ct = default);
    Task<IReadOnlyList<MenuItemBranchAvailability>> ListByBranchAsync(int branchId, CancellationToken ct = default);
    void Add(MenuItemBranchAvailability row);
}
