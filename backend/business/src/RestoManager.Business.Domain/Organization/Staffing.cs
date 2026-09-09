using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Domain.Organization;

public sealed class Department
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Department() { }

    public Department(int branchId, string name, string description)
    {
        BranchId = branchId;
        Update(name, description);
    }

    public void Update(string name, string description)
    {
        Name = OrgGuard.NotBlank(name, nameof(name));
        Description = OrgGuard.Optional(description);
    }
}

/// <summary>Rol/puesto de trabajo (con tarifa horaria). No confundir con los roles de autorización de la Auth API.</summary>
public sealed class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal HourlyRate { get; private set; }

    private Role() { }

    public Role(string name, string description, decimal hourlyRate) => Update(name, description, hourlyRate);

    public void Update(string name, string description, decimal hourlyRate)
    {
        Name = OrgGuard.NotBlank(name, nameof(name));
        Description = OrgGuard.Optional(description);
        HourlyRate = hourlyRate < 0
            ? throw new ArgumentOutOfRangeException(nameof(hourlyRate), "La tarifa horaria no puede ser negativa.")
            : hourlyRate;
    }
}

public sealed class Employee
{
    public int Id { get; private set; }
    public int BranchId { get; private set; }
    public int DepartmentId { get; private set; }
    public int RoleId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public DateOnly HireDate { get; private set; }

    private Employee() { }

    public Employee(
        int branchId, int departmentId, int roleId,
        string firstName, string lastName, string email, string phone, DateOnly hireDate)
    {
        BranchId = branchId;
        DepartmentId = departmentId;
        RoleId = roleId;
        HireDate = hireDate;
        UpdateDetails(firstName, lastName, email, phone);
    }

    public void UpdateDetails(string firstName, string lastName, string email, string phone)
    {
        FirstName = OrgGuard.NotBlank(firstName, nameof(firstName));
        LastName = OrgGuard.NotBlank(lastName, nameof(lastName));
        Email = OrgGuard.Optional(email);
        Phone = OrgGuard.Optional(phone);
    }

    public void Reassign(int departmentId, int roleId)
    {
        DepartmentId = departmentId;
        RoleId = roleId;
    }
}

public sealed class Shift
{
    public int Id { get; private set; }
    public int EmployeeId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public decimal ScheduledHours { get; private set; }

    private Shift() { }

    public static Shift Create(int employeeId, DateTime startTime, DateTime endTime, decimal scheduledHours)
    {
        if (endTime <= startTime)
        {
            throw new DomainRuleException("staffing.invalid_shift", "El fin del turno debe ser posterior al inicio.");
        }
        if (scheduledHours <= 0)
        {
            throw new DomainRuleException("staffing.invalid_shift", "Las horas programadas deben ser mayores que cero.");
        }

        return new Shift
        {
            EmployeeId = employeeId,
            StartTime = DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified),
            EndTime = DateTime.SpecifyKind(endTime, DateTimeKind.Unspecified),
            ScheduledHours = scheduledHours,
        };
    }
}

public enum LeaveStatus { Requested, Approved, Rejected, Cancelled }

public static class LeaveCatalog
{
    public static readonly IReadOnlyList<string> Types = ["VACATION", "SICK", "UNPAID", "OTHER"];

    public static string StatusToDb(LeaveStatus s) => s switch
    {
        LeaveStatus.Requested => "REQUESTED",
        LeaveStatus.Approved => "APPROVED",
        LeaveStatus.Rejected => "REJECTED",
        LeaveStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, null),
    };

    public static LeaveStatus StatusFromDb(string v) => v switch
    {
        "REQUESTED" => LeaveStatus.Requested,
        "APPROVED" => LeaveStatus.Approved,
        "REJECTED" => LeaveStatus.Rejected,
        "CANCELLED" => LeaveStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, "employee_leaves.status inválido"),
    };
}

public sealed class EmployeeLeave
{
    public int Id { get; private set; }
    public int EmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string LeaveType { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; }

    private EmployeeLeave() { }

    public static EmployeeLeave Request(int employeeId, DateOnly startDate, DateOnly endDate, string leaveType)
    {
        if (endDate < startDate)
        {
            throw new DomainRuleException("staffing.invalid_leave", "La fecha de fin no puede ser anterior a la de inicio.");
        }
        if (!LeaveCatalog.Types.Contains(leaveType))
        {
            throw new DomainRuleException(
                "staffing.invalid_leave", $"Tipo de ausencia inválido. Válidos: {string.Join(", ", LeaveCatalog.Types)}.");
        }

        return new EmployeeLeave
        {
            EmployeeId = employeeId,
            StartDate = startDate,
            EndDate = endDate,
            LeaveType = leaveType,
            Status = LeaveStatus.Requested,
        };
    }

    public void Approve() => Transition(LeaveStatus.Approved, from: LeaveStatus.Requested);
    public void Reject() => Transition(LeaveStatus.Rejected, from: LeaveStatus.Requested);

    public void Cancel()
    {
        if (Status is LeaveStatus.Rejected or LeaveStatus.Cancelled)
        {
            throw new DomainRuleException("staffing.invalid_transition", $"No se puede cancelar una ausencia {LeaveCatalog.StatusToDb(Status)}.");
        }
        Status = LeaveStatus.Cancelled;
    }

    private void Transition(LeaveStatus to, LeaveStatus from)
    {
        if (Status != from)
        {
            throw new DomainRuleException(
                "staffing.invalid_transition",
                $"Transición inválida de {LeaveCatalog.StatusToDb(Status)} a {LeaveCatalog.StatusToDb(to)}.");
        }
        Status = to;
    }
}
