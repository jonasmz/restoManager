using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Infrastructure.Persistence.Repositories;

public sealed class RestaurantRepository(BusinessDbContext db) : IRestaurantRepository
{
    public Task<Restaurant?> GetAsync(int id, CancellationToken ct = default)
        => db.Restaurants.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Restaurant>> ListAsync(int skip, int take, CancellationToken ct = default)
        => await db.Restaurants.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default) => db.Restaurants.CountAsync(ct);

    public void Add(Restaurant restaurant) => db.Restaurants.Add(restaurant);
}

public sealed class BranchRepository(BusinessDbContext db) : IBranchRepository
{
    public Task<Branch?> GetAsync(int id, CancellationToken ct = default)
        => db.Branches.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.Branches.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Branch>> ListAsync(int? restaurantId, int skip, int take, CancellationToken ct = default)
        => await Filter(restaurantId).OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(int? restaurantId, CancellationToken ct = default)
        => Filter(restaurantId).CountAsync(ct);

    public void Add(Branch branch) => db.Branches.Add(branch);

    private IQueryable<Branch> Filter(int? restaurantId)
        => restaurantId is { } r ? db.Branches.Where(x => x.RestaurantId == r) : db.Branches;
}

public sealed class RoleRepository(BusinessDbContext db) : IRoleRepository
{
    public Task<Role?> GetAsync(int id, CancellationToken ct = default)
        => db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => db.Roles.AnyAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(int skip, int take, CancellationToken ct = default)
        => await db.Roles.OrderBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default) => db.Roles.CountAsync(ct);

    public void Add(Role role) => db.Roles.Add(role);
}

public sealed class DepartmentRepository(BusinessDbContext db) : IDepartmentRepository
{
    public Task<Department?> GetAsync(int id, CancellationToken ct = default)
        => db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsInBranchAsync(int id, int branchId, CancellationToken ct = default)
        => db.Departments.AnyAsync(x => x.Id == id && x.BranchId == branchId, ct);

    public async Task<IReadOnlyList<Department>> ListByBranchAsync(int branchId, CancellationToken ct = default)
        => await db.Departments.Where(x => x.BranchId == branchId).OrderBy(x => x.Name).ToListAsync(ct);

    public void Add(Department department) => db.Departments.Add(department);
}

public sealed class EmployeeRepository(BusinessDbContext db) : IEmployeeRepository
{
    public Task<Employee?> GetAsync(int id, CancellationToken ct = default)
        => db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Employee>> ListByBranchAsync(
        int branchId, string? search, int skip, int take, CancellationToken ct = default)
        => await Filter(branchId, search)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountByBranchAsync(int branchId, string? search, CancellationToken ct = default)
        => Filter(branchId, search).CountAsync(ct);

    public void Add(Employee employee) => db.Employees.Add(employee);

    private IQueryable<Employee> Filter(int branchId, string? search)
    {
        var q = db.Employees.Where(x => x.BranchId == branchId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.FirstName, s)
                || EF.Functions.ILike(x.LastName, s)
                || EF.Functions.ILike(x.Email, s));
        }
        return q;
    }
}

public sealed class ShiftRepository(BusinessDbContext db) : IShiftRepository
{
    public Task<Shift?> GetAsync(int id, CancellationToken ct = default)
        => db.Shifts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Shift>> ListByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await db.Shifts.Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.StartTime).ToListAsync(ct);

    public void Add(Shift shift) => db.Shifts.Add(shift);
    public void Remove(Shift shift) => db.Shifts.Remove(shift);
}

public sealed class EmployeeLeaveRepository(BusinessDbContext db) : IEmployeeLeaveRepository
{
    public Task<EmployeeLeave?> GetAsync(int id, CancellationToken ct = default)
        => db.EmployeeLeaves.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<EmployeeLeave>> ListByEmployeeAsync(int employeeId, CancellationToken ct = default)
        => await db.EmployeeLeaves.Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.StartDate).ToListAsync(ct);

    public void Add(EmployeeLeave leave) => db.EmployeeLeaves.Add(leave);
}
