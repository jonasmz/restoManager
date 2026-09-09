using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Purchasing;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfig : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.ContactName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Email).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(255).IsRequired();
    }
}

internal sealed class PurchaseOrderConfig : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.Property(x => x.Status)
            .HasConversion(v => v.ToDbValue(), v => PurchaseOrderStatusExtensions.FromDbValue(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Property(x => x.TotalAmount).Money();

        b.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.NoAction);
        b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Fk<PurchaseOrder, Supplier>(nameof(PurchaseOrder.SupplierId));
        b.Fk<PurchaseOrder, Branch>(nameof(PurchaseOrder.BranchId));
    }
}

internal sealed class PurchaseOrderItemConfig : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> b)
    {
        b.Property(x => x.Quantity).Money();
        b.Property(x => x.UnitPrice).Money();
        b.Fk<PurchaseOrderItem, Ingredient>(nameof(PurchaseOrderItem.IngredientId));
    }
}
