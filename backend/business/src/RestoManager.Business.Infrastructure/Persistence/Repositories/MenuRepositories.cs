using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(BusinessDbContext db) : ICategoryRepository
{
    public Task<Category?> GetAsync(int id, CancellationToken ct = default)
        => db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.Categories.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Category>> ListAsync(string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(search).OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
        => Filter(search).CountAsync(ct);

    public void Add(Category category) => db.Categories.Add(category);

    private IQueryable<Category> Filter(string? search)
    {
        var q = db.Categories.AsQueryable();
        return string.IsNullOrWhiteSpace(search)
            ? q
            : q.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim()}%"));
    }
}

public sealed class MenuItemRepository(BusinessDbContext db) : IMenuItemRepository
{
    public Task<MenuItem?> GetAsync(int id, CancellationToken ct = default)
        => db.MenuItems.Include(x => x.Recipe).Include(x => x.Taxes)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.MenuItems.AnyAsync(x => x.Id == id && x.DeletedAt == null, ct);

    public async Task<IReadOnlyList<MenuItem>> ListAsync(
        int? categoryId, string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(categoryId, search)
            .Include(x => x.Recipe).Include(x => x.Taxes)
            .OrderBy(x => x.Name).Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountAsync(int? categoryId, string? search, CancellationToken ct = default)
        => Filter(categoryId, search).CountAsync(ct);

    public void Add(MenuItem menuItem) => db.MenuItems.Add(menuItem);

    private IQueryable<MenuItem> Filter(int? categoryId, string? search)
    {
        var q = db.MenuItems.Where(x => x.DeletedAt == null);
        if (categoryId is { } c)
        {
            q = q.Where(x => x.CategoryId == c);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim()}%"));
        }
        return q;
    }
}

public sealed class KitchenStationRepository(BusinessDbContext db) : IKitchenStationRepository
{
    public Task<KitchenStation?> GetAsync(int id, CancellationToken ct = default)
        => db.KitchenStations.Include(x => x.MenuItems).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<KitchenStation>> ListByBranchAsync(int branchId, CancellationToken ct = default)
        => await db.KitchenStations.Include(x => x.MenuItems)
            .Where(x => x.BranchId == branchId).OrderBy(x => x.Name).ToListAsync(ct);

    public void Add(KitchenStation station) => db.KitchenStations.Add(station);
}

public sealed class MenuItemAvailabilityRepository(BusinessDbContext db) : IMenuItemAvailabilityRepository
{
    public Task<MenuItemBranchAvailability?> GetAsync(int branchId, int menuItemId, CancellationToken ct = default)
        => db.MenuItemBranchAvailabilities
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.MenuItemId == menuItemId, ct);

    public async Task<IReadOnlyList<MenuItemBranchAvailability>> ListByBranchAsync(int branchId, CancellationToken ct = default)
        => await db.MenuItemBranchAvailabilities.Where(x => x.BranchId == branchId).ToListAsync(ct);

    public void Add(MenuItemBranchAvailability row) => db.MenuItemBranchAvailabilities.Add(row);
}

public sealed class TaxRateRepository(BusinessDbContext db) : ITaxRateRepository
{
    public Task<TaxRate?> GetAsync(int id, CancellationToken ct = default)
        => db.TaxRates.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.TaxRates.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<TaxRate>> ListAsync(string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(search).OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(string? search, CancellationToken ct = default)
        => Filter(search).CountAsync(ct);

    public void Add(TaxRate taxRate) => db.TaxRates.Add(taxRate);

    private IQueryable<TaxRate> Filter(string? search)
    {
        var q = db.TaxRates.AsQueryable();
        return string.IsNullOrWhiteSpace(search)
            ? q
            : q.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim()}%"));
    }
}
