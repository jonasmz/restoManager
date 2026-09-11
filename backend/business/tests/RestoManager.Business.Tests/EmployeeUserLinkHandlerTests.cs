using RestoManager.Business.Application.Organization.Employees;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Tests;

internal sealed class FakeEmployees(Employee? employee) : IEmployeeRepository
{
    public Task<Employee?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult(employee);
    public Task<IReadOnlyList<Employee>> ListByBranchAsync(int branchId, string? search, int skip, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Employee>>([]);
    public Task<int> CountByBranchAsync(int branchId, string? search, CancellationToken ct = default) => Task.FromResult(0);
    public void Add(Employee employee) { }
}

public class LinkEmployeeUserHandlerTests
{
    private static Employee NewEmployee() =>
        new(1, 1, 1, "Ana", "Mesera", "ana@resto.local", "", new DateOnly(2026, 1, 1));

    [Fact]
    public async Task Links_existing_employee_and_saves()
    {
        var employee = NewEmployee();
        var uow = new FakeUnitOfWork();
        var handler = new LinkEmployeeUserHandler(new FakeEmployees(employee), uow);

        await handler.HandleAsync(employee.Id, 42);

        Assert.Equal(42, employee.UserId);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Missing_employee_throws_not_found()
    {
        var handler = new LinkEmployeeUserHandler(new FakeEmployees(null), new FakeUnitOfWork());
        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(999, 42));
    }
}

public class UnlinkEmployeeUserHandlerTests
{
    [Fact]
    public async Task Clears_user_id()
    {
        var employee = new Employee(1, 1, 1, "Ana", "Mesera", "ana@resto.local", "", new DateOnly(2026, 1, 1));
        employee.LinkUser(42);
        var handler = new UnlinkEmployeeUserHandler(new FakeEmployees(employee), new FakeUnitOfWork());

        await handler.HandleAsync(employee.Id);

        Assert.Null(employee.UserId);
    }
}

public class EmployeeExistsHandlerTests
{
    [Fact]
    public async Task Returns_true_when_employee_exists()
    {
        var employee = new Employee(1, 1, 1, "Ana", "Mesera", "ana@resto.local", "", new DateOnly(2026, 1, 1));
        var handler = new EmployeeExistsHandler(new FakeEmployees(employee));
        Assert.True(await handler.HandleAsync(employee.Id));
    }

    [Fact]
    public async Task Returns_false_when_missing()
    {
        var handler = new EmployeeExistsHandler(new FakeEmployees(null));
        Assert.False(await handler.HandleAsync(999));
    }
}
