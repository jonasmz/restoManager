using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class IngredientConfig : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        b.Property(x => x.UnitPrice).Money();
    }
}

internal sealed class BranchInventoryConfig : IEntityTypeConfiguration<BranchInventory>
{
    public void Configure(EntityTypeBuilder<BranchInventory> b)
    {
        b.ToTable("branch_inventory", t =>
            t.HasCheckConstraint("ck_branch_inventory_stock_non_negative", "stock_quantity >= 0"));
        b.Property(x => x.StockQuantity).Money().HasDefaultValue(0m);
        b.HasIndex(x => new { x.BranchId, x.IngredientId }).IsUnique();
        b.Fk<BranchInventory, Branch>(nameof(BranchInventory.BranchId));
        b.Fk<BranchInventory, Ingredient>(nameof(BranchInventory.IngredientId));
    }
}

internal sealed class InventoryMovementConfig : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> b)
    {
        b.Property(x => x.MovementType)
            .HasConversion(v => v.ToDbValue(), v => MovementTypeExtensions.FromDbValue(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Property(x => x.Quantity).Money();
        b.Property(x => x.ReferenceType).HasMaxLength(50);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_inventory_movements_type", "movement_type IN ('PURCHASE', 'SALE', 'WASTE', 'ADJUSTMENT')");
            t.HasCheckConstraint("ck_inventory_movements_quantity", "quantity <> 0");
        });

        b.HasIndex(x => new { x.BranchId, x.IngredientId, x.MovementTime });
        b.HasIndex(x => new { x.ReferenceType, x.ReferenceId });

        b.Fk<InventoryMovement, Branch>(nameof(InventoryMovement.BranchId));
        b.Fk<InventoryMovement, Ingredient>(nameof(InventoryMovement.IngredientId));
        b.Fk<InventoryMovement, Employee>(nameof(InventoryMovement.EmployeeId), required: false);
    }
}

internal sealed class WasteLogConfig : IEntityTypeConfiguration<WasteLog>
{
    public void Configure(EntityTypeBuilder<WasteLog> b)
    {
        b.Property(x => x.Reason).HasMaxLength(255).IsRequired();
        b.Property(x => x.Quantity).Money();
        b.Fk<WasteLog, Branch>(nameof(WasteLog.BranchId));
        b.Fk<WasteLog, Ingredient>(nameof(WasteLog.IngredientId));
        b.Fk<WasteLog, Employee>(nameof(WasteLog.LoggedBy));
    }
}
