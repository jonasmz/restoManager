using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class TableConfig : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> b)
    {
        b.Property(x => x.OperationalStatus).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.BranchId, x.Number }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint(
            "ck_tables_operational_status",
            "operational_status IN ('ACTIVE', 'CLEANING', 'OUT_OF_SERVICE')"));
        b.Fk<Table, Branch>(nameof(Table.BranchId));
    }
}

internal sealed class TableSessionConfig : IEntityTypeConfiguration<TableSession>
{
    public void Configure(EntityTypeBuilder<TableSession> b)
    {
        b.HasIndex(x => x.TableId)
            .IsUnique()
            .HasFilter("closed_at IS NULL")
            .HasDatabaseName("ux_table_sessions_one_open_per_table");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_table_sessions_guest_count", "guest_count > 0");
            t.HasCheckConstraint("ck_table_sessions_closed_after_opened", "closed_at IS NULL OR closed_at >= opened_at");
        });
        b.Fk<TableSession, Table>(nameof(TableSession.TableId));
    }
}

internal sealed class ReservationConfig : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Fk<Reservation, Customer>(nameof(Reservation.CustomerId));
        b.Fk<Reservation, Branch>(nameof(Reservation.BranchId));
        b.Fk<Reservation, Table>(nameof(Reservation.TableId));
    }
}
