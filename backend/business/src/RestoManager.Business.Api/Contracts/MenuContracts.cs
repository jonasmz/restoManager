namespace RestoManager.Business.Api.Contracts;

// ---- Categorías ----
public sealed record SaveCategoryRequest(string Name, string Description);

// ---- Platos ----
public sealed record RecipeLineBody(int IngredientId, decimal QuantityRequired);

/// <summary>
/// Alta/edición de plato. <see cref="Recipe"/> y <see cref="TaxRateIds"/> son opcionales:
/// si vienen (aunque sea vacíos) reemplazan la receta / los impuestos del plato, de modo
/// que la ficha persiste en una sola operación. Si son <c>null</c> no se tocan.
/// </summary>
public sealed record SaveMenuItemRequest(
    int CategoryId, string Name, string Description, decimal Price, bool IsAvailable,
    IReadOnlyList<RecipeLineBody>? Recipe, IReadOnlyList<int>? TaxRateIds);

public sealed record SetAvailabilityRequest(bool IsAvailable);

// ---- Estaciones de cocina ----
public sealed record SaveKitchenStationRequest(string Name, string Description);
public sealed record SetStationMenuItemsRequest(IReadOnlyList<int> MenuItemIds);

// ---- Tasas de impuesto ----
public sealed record SaveTaxRateRequest(string Name, decimal Rate);
