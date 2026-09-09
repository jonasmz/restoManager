using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Infrastructure.Persistence.Configurations;

internal sealed class RestaurantConfig : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(255).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Email).HasMaxLength(100).IsRequired();
        b.Property(x => x.TaxNumber).HasMaxLength(50).IsRequired();
    }
}

internal sealed class BranchConfig : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(255).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Email).HasMaxLength(100).IsRequired();
        b.Fk<Branch, Restaurant>(nameof(Branch.RestaurantId));
    }
}

internal sealed class DepartmentConfig : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.Fk<Department, Branch>(nameof(Department.BranchId));
    }
}

internal sealed class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255).IsRequired();
        b.Property(x => x.HourlyRate).Money();
    }
}

internal sealed class EmployeeConfig : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(100).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Fk<Employee, Branch>(nameof(Employee.BranchId));
        b.Fk<Employee, Department>(nameof(Employee.DepartmentId));
        b.Fk<Employee, Role>(nameof(Employee.RoleId));
    }
}

internal sealed class ShiftConfig : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> b)
    {
        b.Property(x => x.ScheduledHours).HasPrecision(5, 2);
        b.Fk<Shift, Employee>(nameof(Shift.EmployeeId));
    }
}

internal sealed class EmployeeLeaveConfig : IEntityTypeConfiguration<EmployeeLeave>
{
    public void Configure(EntityTypeBuilder<EmployeeLeave> b)
    {
        b.Property(x => x.LeaveType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status)
            .HasConversion(v => LeaveCatalog.StatusToDb(v), v => LeaveCatalog.StatusFromDb(v))
            .HasMaxLength(50)
            .IsRequired();
        b.Fk<EmployeeLeave, Employee>(nameof(EmployeeLeave.EmployeeId));
    }
}
