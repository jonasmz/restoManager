namespace RestoManager.Business.Domain.Menu;

// Módulo Menú y cocina. Solo esquema en la Fase 3 (recipe_items lo consume el
// cálculo de consumo de la Fase 6). La Fase 4 añade CRUD y edición de recetas.

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class MenuItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed class RecipeItem
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public int IngredientId { get; set; }
    public decimal QuantityRequired { get; set; }
}

public sealed class KitchenStation
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class StationMenuItem
{
    public int Id { get; set; }
    public int StationId { get; set; }
    public int MenuItemId { get; set; }
}
