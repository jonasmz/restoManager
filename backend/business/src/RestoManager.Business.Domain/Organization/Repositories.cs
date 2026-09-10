namespace RestoManager.Business.Domain.Organization;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Restaurant>> ListAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    void Add(Restaurant restaurant);
}

public interface IBranchRepository
{
    Task<Branch?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>Sucursal cuyo <see cref="Branch.PublicSlug"/> coincide (carta pública, Fase 11).</summary>
    Task<Branch?> GetByPublicSlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<Branch>> ListAsync(int? restaurantId, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(int? restaurantId, CancellationToken ct = default);
    void Add(Branch branch);
}

public interface IRoleRepository
{
    Task<Role?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    void Add(Role role);
}

public interface IDepartmentRepository
{
    Task<Department?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsInBranchAsync(int id, int branchId, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> ListByBranchAsync(int branchId, CancellationToken ct = default);
    void Add(Department department);
}

public interface IEmployeeRepository
{
    Task<Employee?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> ListByBranchAsync(int branchId, string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountByBranchAsync(int branchId, string? search, CancellationToken ct = default);
    void Add(Employee employee);
}

public interface IShiftRepository
{
    Task<Shift?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Shift>> ListByEmployeeAsync(int employeeId, CancellationToken ct = default);
    void Add(Shift shift);
    void Remove(Shift shift);
}

public interface IEmployeeLeaveRepository
{
    Task<EmployeeLeave?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeLeave>> ListByEmployeeAsync(int employeeId, CancellationToken ct = default);
    void Add(EmployeeLeave leave);
}
