using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Domain.Menu;

// Módulo Menú y cocina. La Fase 4 le añade comportamiento (CRUD, edición de recetas
// e impuestos). `recipe_items` lo consume el descuento de stock por receta de la Fase 6.

internal static class MenuGuard
{
    public static string NotBlank(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"'{name}' es obligatorio.", name)
            : value.Trim();

    public static string Optional(string? value) => value?.Trim() ?? string.Empty;
}

/// <summary>Categoría de la carta. Entidad global (sin sucursal).</summary>
public sealed class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Category() { }

    public Category(string name, string description) => Update(name, description);

    public void Update(string name, string description)
    {
        Name = MenuGuard.NotBlank(name, nameof(name));
        Description = MenuGuard.Optional(description);
    }
}

/// <summary>
/// Plato de la carta. Entidad global: la carta y el precio son iguales en todas las
/// sucursales (decisión Fase 4). La disponibilidad puede ajustarse por sucursal en
/// <see cref="MenuItemBranchAvailability"/>; <see cref="IsAvailable"/> es el valor por
/// defecto. Los impuestos son <b>inclusivos</b>: <see cref="Price"/> ya los contiene.
/// Agrega su receta (<see cref="Recipe"/>) y sus impuestos (<see cref="Taxes"/>); ambos
/// se reemplazan en bloque para que la ficha del plato persista en una sola operación.
/// </summary>
public sealed class MenuItem
{
    private readonly List<RecipeItem> _recipe = [];
    private readonly List<MenuItemTax> _taxes = [];

    public int Id { get; private set; }
    public int CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    /// <summary>
    /// Clave de la imagen ilustrativa del plato (nombre de archivo en el almacén de
    /// imágenes, Fase 11). <c>null</c> = sin imagen. La URL pública se deriva de la clave.
    /// </summary>
    public string? ImageKey { get; private set; }

    public IReadOnlyList<RecipeItem> Recipe => _recipe;
    public IReadOnlyList<MenuItemTax> Taxes => _taxes;

    private MenuItem() { }

    public MenuItem(int categoryId, string name, string description, decimal price, bool isAvailable)
    {
        SetCategory(categoryId);
        UpdateDetails(name, description, price, isAvailable);
    }

    public void SetCategory(int categoryId)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "La categoría es obligatoria.");
        }
        CategoryId = categoryId;
    }

    public void UpdateDetails(string name, string description, decimal price, bool isAvailable)
    {
        Name = MenuGuard.NotBlank(name, nameof(name));
        Description = MenuGuard.Optional(description);
        SetPrice(price);
        IsAvailable = isAvailable;
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainRuleException("menu.invalid_price", "El precio no puede ser negativo.");
        }
        Price = price;
    }

    /// <summary>Asocia la imagen identificada por <paramref name="key"/> (Fase 11).</summary>
    public void SetImage(string key) => ImageKey = MenuGuard.NotBlank(key, nameof(key));

    /// <summary>Quita la imagen del plato.</summary>
    public void ClearImage() => ImageKey = null;

    /// <summary>
    /// Reemplaza toda la receta. Rechaza ingredientes repetidos (§7.5,
    /// <c>UNIQUE(menu_item_id, ingredient_id)</c>) y cantidades ≤ 0. <c>isPublic</c>
    /// controla si el ingrediente se muestra en la carta pública (issue #34).
    /// </summary>
    public void SetRecipe(IEnumerable<(int ingredientId, decimal quantityRequired, bool isPublic)> lines)
    {
        var incoming = lines.ToList();
        var duplicated = incoming.GroupBy(l => l.ingredientId).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new DomainRuleException(
                "menu.recipe_duplicate_ingredient",
                $"El ingrediente {duplicated.Key} está repetido en la receta.");
        }

        _recipe.Clear();
        foreach (var (ingredientId, quantity, isPublic) in incoming)
        {
            _recipe.Add(RecipeItem.Create(ingredientId, quantity, isPublic));
        }
    }

    /// <summary>Reemplaza los impuestos del plato. Rechaza tasas repetidas (<c>UNIQUE(menu_item_id, tax_rate_id)</c>).</summary>
    public void SetTaxes(IEnumerable<int> taxRateIds)
    {
        var incoming = taxRateIds.ToList();
        var duplicated = incoming.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new DomainRuleException("menu.tax_duplicate", $"La tasa de impuesto {duplicated.Key} está repetida.");
        }

        _taxes.Clear();
        foreach (var taxRateId in incoming)
        {
            if (taxRateId <= 0)
            {
                throw new DomainRuleException("menu.tax_invalid", "Tasa de impuesto inválida.");
            }
            _taxes.Add(MenuItemTax.Create(taxRateId));
        }
    }
}

/// <summary>Línea de receta: qué ingrediente y cuánto consume un plato.</summary>
public sealed class RecipeItem
{
    public int Id { get; private set; }
    public int MenuItemId { get; private set; }
    public int IngredientId { get; private set; }
    public decimal QuantityRequired { get; private set; }

    /// <summary>
    /// Si el nombre del ingrediente se muestra en la carta pública (Fase 11 / issue #34).
    /// Por defecto <c>true</c>: al no marcar nada, la receta se ve completa.
    /// </summary>
    public bool IsPublic { get; private set; } = true;

    private RecipeItem() { }

    internal static RecipeItem Create(int ingredientId, decimal quantityRequired, bool isPublic = true)
    {
        if (ingredientId <= 0)
        {
            throw new DomainRuleException("menu.recipe_invalid_ingredient", "Ingrediente inválido.");
        }
        if (quantityRequired <= 0)
        {
            throw new DomainRuleException(
                "menu.recipe_invalid_quantity", "La cantidad requerida debe ser mayor que cero.");
        }
        return new RecipeItem { IngredientId = ingredientId, QuantityRequired = quantityRequired, IsPublic = isPublic };
    }
}

/// <summary>Estación de cocina. Pertenece a una sucursal. Agrega los platos que prepara.</summary>
public sealed class KitchenStation
{
    private readonly List<StationMenuItem> _menuItems = [];

    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public IReadOnlyList<StationMenuItem> MenuItems => _menuItems;

    private KitchenStation() { }

    public KitchenStation(int branchId, string name, string description)
    {
        if (branchId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(branchId), "La sucursal es obligatoria.");
        }
        BranchId = branchId;
        Update(name, description);
    }

    public void Update(string name, string description)
    {
        Name = MenuGuard.NotBlank(name, nameof(name));
        Description = MenuGuard.Optional(description);
    }

    /// <summary>Reemplaza los platos asignados a la estación. Rechaza platos repetidos (<c>UNIQUE(station_id, menu_item_id)</c>).</summary>
    public void SetMenuItems(IEnumerable<int> menuItemIds)
    {
        var incoming = menuItemIds.ToList();
        var duplicated = incoming.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new DomainRuleException(
                "menu.station_duplicate_item", $"El plato {duplicated.Key} está asignado dos veces a la estación.");
        }

        _menuItems.Clear();
        foreach (var menuItemId in incoming)
        {
            if (menuItemId <= 0)
            {
                throw new DomainRuleException("menu.station_invalid_item", "Plato inválido.");
            }
            _menuItems.Add(StationMenuItem.Create(menuItemId));
        }
    }
}

public sealed class StationMenuItem
{
    public int Id { get; private set; }
    public int StationId { get; private set; }
    public int MenuItemId { get; private set; }

    private StationMenuItem() { }

    internal static StationMenuItem Create(int menuItemId) => new() { MenuItemId = menuItemId };
}

/// <summary>
/// Disponibilidad de un plato en una sucursal concreta (desvío del esquema, Fase 4).
/// Si no hay fila para (sucursal, plato) se hereda <see cref="MenuItem.IsAvailable"/>.
/// </summary>
public sealed class MenuItemBranchAvailability
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int MenuItemId { get; private set; }
    public bool IsAvailable { get; private set; }

    private MenuItemBranchAvailability() { }

    public MenuItemBranchAvailability(int branchId, int menuItemId, bool isAvailable)
    {
        if (branchId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(branchId), "La sucursal es obligatoria.");
        }
        if (menuItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(menuItemId), "El plato es obligatorio.");
        }
        BranchId = branchId;
        MenuItemId = menuItemId;
        IsAvailable = isAvailable;
    }

    public void Set(bool isAvailable) => IsAvailable = isAvailable;
}
