using FluentValidation;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Application.Menu.Items;

public sealed record RecipeLineDto(int IngredientId, decimal QuantityRequired, bool IsPublic = true);

public sealed record MenuItemDto(
    int Id, int CategoryId, string Name, string Description, decimal Price, bool IsAvailable,
    IReadOnlyList<RecipeLineDto> Recipe, IReadOnlyList<int> TaxRateIds, string? ImageUrl);

/// <summary>
/// Traduce la clave de imagen de un plato (<see cref="Domain.Menu.MenuItem.ImageKey"/>) a
/// la ruta pública servida por la API (<c>/media/menu/&lt;clave&gt;</c>). El frontend le
/// antepone la base de la API. Fase 11.
/// </summary>
public static class MenuImagePath
{
    public const string Prefix = "/media/menu";

    public static string? For(string? key) => string.IsNullOrEmpty(key) ? null : $"{Prefix}/{key}";
}

public sealed record RecipeCostLineDto(
    int IngredientId, string IngredientName, decimal QuantityRequired, decimal UnitPrice, decimal LineCost);

public sealed record MenuItemCostDto(int MenuItemId, decimal Cost, IReadOnlyList<RecipeCostLineDto> Lines);

// ---- Guardar (crea o actualiza; receta e impuestos opcionales se reemplazan en bloque) ----
public sealed record SaveMenuItemCommand(
    int? Id, int CategoryId, string Name, string Description, decimal Price, bool IsAvailable,
    IReadOnlyList<RecipeLineDto>? Recipe, IReadOnlyList<int>? TaxRateIds);

public sealed class SaveMenuItemValidator : AbstractValidator<SaveMenuItemCommand>
{
    public SaveMenuItemValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Recipe).ChildRules(line =>
        {
            line.RuleFor(l => l.IngredientId).GreaterThan(0);
            line.RuleFor(l => l.QuantityRequired).GreaterThan(0);
        });
        RuleForEach(x => x.TaxRateIds).GreaterThan(0);
    }
}

public sealed class SaveMenuItemHandler(
    IMenuItemRepository menuItems,
    ICategoryRepository categories,
    IIngredientRepository ingredients,
    ITaxRateRepository taxRates,
    IUnitOfWork unitOfWork,
    IValidator<SaveMenuItemCommand> validator)
{
    public async Task<int> HandleAsync(SaveMenuItemCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);

        if (!await categories.ExistsAsync(command.CategoryId, ct))
        {
            throw new NotFoundException("categoría", command.CategoryId);
        }

        MenuItem entity;
        if (command.Id is { } id)
        {
            entity = await menuItems.GetAsync(id, ct) ?? throw new NotFoundException("plato", id);
            entity.SetCategory(command.CategoryId);
            entity.UpdateDetails(command.Name, command.Description, command.Price, command.IsAvailable);
        }
        else
        {
            entity = new MenuItem(command.CategoryId, command.Name, command.Description, command.Price, command.IsAvailable);
            menuItems.Add(entity);
        }

        if (command.Recipe is { } recipe)
        {
            foreach (var line in recipe)
            {
                if (!await ingredients.ExistsAsync(line.IngredientId, ct))
                {
                    throw new NotFoundException("ingrediente", line.IngredientId);
                }
            }
            entity.SetRecipe(recipe.Select(l => (l.IngredientId, l.QuantityRequired, l.IsPublic)));
        }

        if (command.TaxRateIds is { } taxIds)
        {
            foreach (var taxRateId in taxIds)
            {
                if (!await taxRates.ExistsAsync(taxRateId, ct))
                {
                    throw new NotFoundException("tasa de impuesto", taxRateId);
                }
            }
            entity.SetTaxes(taxIds);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

// ---- Consultas ----
public sealed record ListMenuItemsQuery(int? CategoryId, string? Search, int Page = 1, int PageSize = 20);

public sealed class ListMenuItemsHandler(IMenuItemRepository menuItems)
{
    public async Task<PagedResult<MenuItemDto>> HandleAsync(ListMenuItemsQuery query, CancellationToken ct = default)
    {
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await menuItems.ListAsync(query.CategoryId, query.Search, page.Skip, page.Take, ct);
        var total = await menuItems.CountAsync(query.CategoryId, query.Search, ct);
        return new PagedResult<MenuItemDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static MenuItemDto Map(MenuItem m) => new(
        m.Id, m.CategoryId, m.Name, m.Description, m.Price, m.IsAvailable,
        m.Recipe.Select(r => new RecipeLineDto(r.IngredientId, r.QuantityRequired, r.IsPublic)).ToList(),
        m.Taxes.Select(t => t.TaxRateId).ToList(),
        MenuImagePath.For(m.ImageKey));
}

public sealed class GetMenuItemHandler(IMenuItemRepository menuItems)
{
    public async Task<MenuItemDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var m = await menuItems.GetAsync(id, ct) ?? throw new NotFoundException("plato", id);
        return ListMenuItemsHandler.Map(m);
    }
}

// ---- Eliminar (baja lógica, issue #47; no afecta a los ingredientes de la receta) ----
public sealed class DeleteMenuItemHandler(IMenuItemRepository menuItems, IClock clock, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var item = await menuItems.GetAsync(id, ct) ?? throw new NotFoundException("plato", id);
        item.Delete(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>Costo teórico del plato = Σ(<c>quantity_required</c> × <c>ingredients.unit_price</c>) (apoyo para la Fase 6).</summary>
public sealed class GetMenuItemCostHandler(IMenuItemRepository menuItems, IIngredientRepository ingredients)
{
    public async Task<MenuItemCostDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var item = await menuItems.GetAsync(id, ct) ?? throw new NotFoundException("plato", id);

        var lines = new List<RecipeCostLineDto>(item.Recipe.Count);
        foreach (var r in item.Recipe)
        {
            var ingredient = await ingredients.GetAsync(r.IngredientId, ct)
                ?? throw new NotFoundException("ingrediente", r.IngredientId);
            var lineCost = r.QuantityRequired * ingredient.UnitPrice;
            lines.Add(new RecipeCostLineDto(
                ingredient.Id, ingredient.Name, r.QuantityRequired, ingredient.UnitPrice, lineCost));
        }

        return new MenuItemCostDto(item.Id, lines.Sum(l => l.LineCost), lines);
    }
}
