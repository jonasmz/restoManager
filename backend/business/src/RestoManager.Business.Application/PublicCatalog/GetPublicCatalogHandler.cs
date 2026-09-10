using RestoManager.Business.Application.Menu.Items;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.PublicCatalog;

/// <summary>
/// Arma la carta pública de la sucursal identificada por su slug (Fase 11).
///
/// Disponibilidad efectiva por sucursal: si existe fila en
/// <see cref="MenuItemBranchAvailability"/> para el plato, manda ese valor; si no,
/// se hereda <see cref="MenuItem.IsAvailable"/>. Solo se devuelven platos visibles y
/// las categorías que tienen al menos un plato visible.
/// </summary>
public sealed class GetPublicCatalogHandler(
    IBranchRepository branches,
    IRestaurantRepository restaurants,
    ICategoryRepository categories,
    IMenuItemRepository menuItems,
    IIngredientRepository ingredients,
    IMenuItemAvailabilityRepository availability)
{
    // La carta de un restaurante es chica; el frontend filtra y busca sobre este único payload.
    private const int MaxRows = 2000;

    public async Task<PublicCatalogDto> HandleAsync(string slug, CancellationToken ct = default)
    {
        var normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();
        var branch = await branches.GetByPublicSlugAsync(normalized, ct)
            ?? throw new NotFoundException("carta", slug ?? string.Empty);

        var restaurant = await restaurants.GetAsync(branch.RestaurantId, ct)
            ?? throw new NotFoundException("restaurante", branch.RestaurantId);

        var overrides = (await availability.ListByBranchAsync(branch.Id, ct))
            .ToDictionary(a => a.MenuItemId, a => a.IsAvailable);

        var categoryNames = (await categories.ListAsync(null, 0, MaxRows, ct))
            .ToDictionary(c => c.Id, c => c.Name);
        var ingredientNames = (await ingredients.ListAsync(null, 0, MaxRows, ct))
            .ToDictionary(i => i.Id, i => i.Name);

        var allItems = await menuItems.ListAsync(null, null, 0, MaxRows, ct);

        var items = allItems
            .Where(m => overrides.TryGetValue(m.Id, out var flag) ? flag : m.IsAvailable)
            .Select(m => new PublicCatalogItemDto(
                m.Id,
                m.CategoryId,
                categoryNames.GetValueOrDefault(m.CategoryId, string.Empty),
                m.Name,
                m.Description,
                m.Price,
                MenuImagePath.For(m.ImageKey),
                m.Recipe
                    .Select(r => ingredientNames.GetValueOrDefault(r.IngredientId))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n!)
                    .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()))
            .OrderBy(i => i.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var visibleCategoryIds = items.Select(i => i.CategoryId).ToHashSet();
        var categoryList = visibleCategoryIds
            .Select(id => new PublicCategoryDto(id, categoryNames.GetValueOrDefault(id, string.Empty)))
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new PublicCatalogDto(
            new PublicRestaurantDto(restaurant.Name),
            new PublicBranchDto(branch.Name, branch.Address),
            categoryList,
            items);
    }
}
