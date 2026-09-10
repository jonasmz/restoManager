using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Tests;

public class EmployeeLeaveTests
{
    private static EmployeeLeave NewLeave() =>
        EmployeeLeave.Request(1, new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12), "VACATION");

    [Fact]
    public void Request_starts_requested()
        => Assert.Equal(LeaveStatus.Requested, NewLeave().Status);

    [Fact]
    public void End_before_start_is_rejected()
        => Assert.Throws<DomainRuleException>(() =>
            EmployeeLeave.Request(1, new DateOnly(2026, 9, 12), new DateOnly(2026, 9, 10), "VACATION"));

    [Fact]
    public void Unknown_type_is_rejected()
        => Assert.Throws<DomainRuleException>(() =>
            EmployeeLeave.Request(1, new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12), "HOLIDAY"));

    [Fact]
    public void Approve_then_cannot_reject()
    {
        var leave = NewLeave();
        leave.Approve();
        Assert.Equal(LeaveStatus.Approved, leave.Status);
        Assert.Throws<DomainRuleException>(leave.Reject);
    }

    [Fact]
    public void Rejected_cannot_be_cancelled()
    {
        var leave = NewLeave();
        leave.Reject();
        Assert.Throws<DomainRuleException>(leave.Cancel);
    }
}

public class ShiftTests
{
    [Fact]
    public void End_after_start_and_positive_hours_ok()
    {
        var shift = Shift.Create(1, new DateTime(2026, 9, 10, 8, 0, 0), new DateTime(2026, 9, 10, 16, 0, 0), 8m);
        Assert.Equal(8m, shift.ScheduledHours);
        Assert.Equal(DateTimeKind.Unspecified, shift.StartTime.Kind);
    }

    [Fact]
    public void End_not_after_start_is_rejected()
        => Assert.Throws<DomainRuleException>(() =>
            Shift.Create(1, new DateTime(2026, 9, 10, 16, 0, 0), new DateTime(2026, 9, 10, 16, 0, 0), 1m));

    [Fact]
    public void Non_positive_hours_rejected()
        => Assert.Throws<DomainRuleException>(() =>
            Shift.Create(1, new DateTime(2026, 9, 10, 8, 0, 0), new DateTime(2026, 9, 10, 16, 0, 0), 0m));
}

public class OrgEntityValidationTests
{
    [Fact]
    public void Role_rejects_negative_rate()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Role("Mesero", "", -1m));

    [Fact]
    public void Restaurant_requires_name_and_address()
    {
        Assert.Throws<ArgumentException>(() => new Restaurant("", "dir", "", "", ""));
        Assert.Throws<ArgumentException>(() => new Restaurant("Demo", "  ", "", "", ""));
    }
}

public class BranchPublicSlugTests
{
    private static Branch NewBranch() =>
        new(1, "Centro", "Calle 1", "", "", new TimeOnly(8, 0), new TimeOnly(23, 0));

    [Fact]
    public void Sets_and_normalizes_slug_to_lowercase()
    {
        var branch = NewBranch();
        branch.SetPublicSlug("  Centro-Norte  ");
        Assert.Equal("centro-norte", branch.PublicSlug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_clears_slug(string? value)
    {
        var branch = NewBranch();
        branch.SetPublicSlug("centro");
        branch.SetPublicSlug(value);
        Assert.Null(branch.PublicSlug);
    }

    [Theory]
    [InlineData("ab")]             // muy corto
    [InlineData("-centro")]        // guion al inicio
    [InlineData("centro-")]        // guion al final
    [InlineData("centro norte")]   // espacio
    [InlineData("Centro_Norte")]   // guion bajo
    public void Rejects_invalid_format(string value)
    {
        var ex = Assert.Throws<DomainRuleException>(() => NewBranch().SetPublicSlug(value));
        Assert.Equal("branch.invalid_slug", ex.Code);
    }
}
