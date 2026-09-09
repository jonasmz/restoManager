namespace RestoManager.Business.Api.Contracts;

public sealed record SaveRestaurantRequest(string Name, string Address, string Phone, string Email, string TaxNumber);

public sealed record SaveBranchRequest(
    int RestaurantId, string Name, string Address, string Phone, string Email,
    TimeOnly OpeningTime, TimeOnly ClosingTime);

public sealed record SaveRoleRequest(string Name, string Description, decimal HourlyRate);

public sealed record SaveDepartmentRequest(string Name, string Description);

public sealed record SaveEmployeeRequest(
    int DepartmentId, int RoleId, string FirstName, string LastName, string Email, string Phone, DateOnly HireDate);

public sealed record AddShiftRequest(DateTime StartTime, DateTime EndTime, decimal ScheduledHours);

public sealed record RequestLeaveRequest(DateOnly StartDate, DateOnly EndDate, string LeaveType);
