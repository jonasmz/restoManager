using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(x => x.Channel)
            .HasConversion(v => v.ToDbValue(), v => OrderChannelExtensions.FromDbValue(v))
            .HasMaxLength(20)
            .IsRequired();
        b.Property(x => x.Status)
            .HasConversion(v => v.ToDbValue(), v => OrderStatusExtensions.FromDbValue(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Property(x => x.TotalAmount).Money();

        // Derivados: no se persisten.
        b.Ignore(x => x.ItemsSubtotal);
        b.Ignore(x => x.DiscountTotal);
        b.Ignore(x => x.ConfirmedPaid);
        b.Ignore(x => x.Balance);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_orders_channel", "channel IN ('MESA', 'BARRA', 'TAKEAWAY', 'DELIVERY')");
            t.HasCheckConstraint(
                "ck_orders_channel_table",
                "(channel = 'MESA' AND table_id IS NOT NULL) OR " +
                "(channel <> 'MESA' AND table_id IS NULL AND table_session_id IS NULL)");
        });

        // Colecciones hijas del agregado. FK NO ACTION en BD (como el DDL); EF borra
        // los huérfanos al removerlos del agregado (patrón Fase 4).
        b.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(x => x.Discounts)
            .WithOne()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(x => x.Discounts).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(x => x.Payments)
            .WithOne()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.ClientCascade);
        b.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Fk<Order, Branch>(nameof(Order.BranchId));
        b.Fk<Order, Employee>(nameof(Order.EmployeeId));
        b.Fk<Order, Table>(nameof(Order.TableId), required: false);
        b.Fk<Order, Customer>(nameof(Order.CustomerId), required: false);
        b.Fk<Order, TableSession>(nameof(Order.TableSessionId), required: false);
    }
}

internal sealed class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(x => x.UnitPrice).Money();
        b.Property(x => x.Notes).HasMaxLength(255);
        b.Ignore(x => x.LineTotal);
        b.Fk<OrderItem, MenuItem>(nameof(OrderItem.MenuItemId));
    }
}

internal sealed class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(x => x.PaymentMethod)
            .HasConversion(v => v.ToDbValue(), v => PaymentMethodExtensions.FromDbValue(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Property(x => x.Status)
            .HasConversion(v => v.ToDbValue(), v => PaymentStatusExtensions.FromDbValue(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Property(x => x.Amount).Money();
    }
}

internal sealed class DiscountConfig : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Type)
            .HasConversion(v => v.ToDbValue(), v => DiscountTypeExtensions.FromDbValue(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Property(x => x.Value).Money();
    }
}

internal sealed class OrderDiscountConfig : IEntityTypeConfiguration<OrderDiscount>
{
    public void Configure(EntityTypeBuilder<OrderDiscount> b)
    {
        b.Property(x => x.AppliedAmount).Money();
        b.HasIndex(x => new { x.OrderId, x.DiscountId }).IsUnique();
        b.Fk<OrderDiscount, Discount>(nameof(OrderDiscount.DiscountId));
    }
}
