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
        b.Property(x => x.ImageKey).HasMaxLength(200); // Fase 11: nullable, sin imagen por defecto
        b.Fk<MenuItem, Category>(nameof(MenuItem.CategoryId));

        b.HasMany(x => x.Recipe)
            .WithOne()
            .HasForeignKey(r => r.MenuItemId)
            .OnDelete(DeleteBehavior.ClientCascade); // FK NO ACTION en BD; EF borra los huérfanos al reemplazar el bloque
        b.Navigation(x => x.Recipe).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(x => x.Taxes)
            .WithOne()
            .HasForeignKey(t => t.MenuItemId)
            .OnDelete(DeleteBehavior.ClientCascade); // FK NO ACTION en BD; EF borra los huérfanos al reemplazar el bloque
        b.Navigation(x => x.Taxes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RecipeItemConfig : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> b)
    {
        b.Property(x => x.QuantityRequired).Money();
        b.HasIndex(x => new { x.MenuItemId, x.IngredientId }).IsUnique();
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

        b.HasMany(x => x.MenuItems)
            .WithOne()
            .HasForeignKey(m => m.StationId)
            .OnDelete(DeleteBehavior.ClientCascade); // FK NO ACTION en BD; EF borra los huérfanos al reemplazar el bloque
        b.Navigation(x => x.MenuItems).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StationMenuItemConfig : IEntityTypeConfiguration<StationMenuItem>
{
    public void Configure(EntityTypeBuilder<StationMenuItem> b)
    {
        b.HasIndex(x => new { x.StationId, x.MenuItemId }).IsUnique();
        b.Fk<StationMenuItem, MenuItem>(nameof(StationMenuItem.MenuItemId));
    }
}

internal sealed class MenuItemBranchAvailabilityConfig : IEntityTypeConfiguration<MenuItemBranchAvailability>
{
    public void Configure(EntityTypeBuilder<MenuItemBranchAvailability> b)
    {
        b.ToTable("menu_item_branch_availability");
        b.HasIndex(x => new { x.BranchId, x.MenuItemId }).IsUnique();
        b.Fk<MenuItemBranchAvailability, Branch>(nameof(MenuItemBranchAvailability.BranchId));
        b.Fk<MenuItemBranchAvailability, MenuItem>(nameof(MenuItemBranchAvailability.MenuItemId));
    }
}
