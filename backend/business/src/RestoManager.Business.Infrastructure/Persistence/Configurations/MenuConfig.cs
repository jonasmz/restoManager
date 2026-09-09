using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
    }
}

internal sealed class MenuItemConfig : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.Property(x => x.Price).Money();
        b.Fk<MenuItem, Category>(nameof(MenuItem.CategoryId));
    }
}

internal sealed class RecipeItemConfig : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> b)
    {
        b.Property(x => x.QuantityRequired).Money();
        b.HasIndex(x => new { x.MenuItemId, x.IngredientId }).IsUnique();
        b.Fk<RecipeItem, MenuItem>(nameof(RecipeItem.MenuItemId));
        b.Fk<RecipeItem, Ingredient>(nameof(RecipeItem.IngredientId));
    }
}

internal sealed class KitchenStationConfig : IEntityTypeConfiguration<KitchenStation>
{
    public void Configure(EntityTypeBuilder<KitchenStation> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.Fk<KitchenStation, Branch>(nameof(KitchenStation.BranchId));
    }
}

internal sealed class StationMenuItemConfig : IEntityTypeConfiguration<StationMenuItem>
{
    public void Configure(EntityTypeBuilder<StationMenuItem> b)
    {
        b.HasIndex(x => new { x.StationId, x.MenuItemId }).IsUnique();
        b.Fk<StationMenuItem, KitchenStation>(nameof(StationMenuItem.StationId));
        b.Fk<StationMenuItem, MenuItem>(nameof(StationMenuItem.MenuItemId));
    }
}
