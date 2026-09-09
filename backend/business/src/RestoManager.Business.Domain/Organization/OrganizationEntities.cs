namespace RestoManager.Business.Domain.Organization;

// Entidades del módulo Organización. En la Fase 3 solo se usan como destino de
// claves foráneas (branch_id, employee_id). La Fase 2 les añade comportamiento y CRUD.

public sealed class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
}

public sealed class Branch
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
}

public sealed class Department
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
}

public sealed class Employee
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int DepartmentId { get; set; }
    public int RoleId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
}

public sealed class Shift
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal ScheduledHours { get; set; }
}

public sealed class EmployeeLeave
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
