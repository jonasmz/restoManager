using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Delivery;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Sales;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryDriverConfig : IEntityTypeConfiguration<DeliveryDriver>
{
    public void Configure(EntityTypeBuilder<DeliveryDriver> b)
    {
        b.Property(x => x.VehicleType).HasMaxLength(50).IsRequired();
        b.Property(x => x.LicensePlate).HasMaxLength(20).IsRequired();
        b.Fk<DeliveryDriver, Employee>(nameof(DeliveryDriver.EmployeeId));
    }
}

internal sealed class DeliveryConfig : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> b)
    {
        b.Property(x => x.DeliveryAddress).HasMaxLength(255).IsRequired();
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Fk<Delivery, Order>(nameof(Delivery.OrderId));
        b.Fk<Delivery, DeliveryDriver>(nameof(Delivery.DriverId));
    }
}

internal sealed class TaxRateConfig : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Rate).HasPrecision(5, 2);
    }
}

internal sealed class MenuItemTaxConfig : IEntityTypeConfiguration<MenuItemTax>
{
    public void Configure(EntityTypeBuilder<MenuItemTax> b)
    {
        b.HasIndex(x => new { x.MenuItemId, x.TaxRateId }).IsUnique();
        b.Fk<MenuItemTax, MenuItem>(nameof(MenuItemTax.MenuItemId));
        b.Fk<MenuItemTax, TaxRate>(nameof(MenuItemTax.TaxRateId));
    }
}
