using FluentValidation;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Common;
using RestoManager.Business.Domain.Organization;

namespace RestoManager.Business.Application.Organization.Employees;

public sealed record EmployeeDto(
    int Id, int BranchId, int DepartmentId, int RoleId,
    string FirstName, string LastName, string Email, string Phone, DateOnly HireDate, int? UserId);

public sealed record ShiftDto(int Id, int EmployeeId, DateTime StartTime, DateTime EndTime, decimal ScheduledHours);

public sealed record LeaveDto(
    int Id, int EmployeeId, DateOnly StartDate, DateOnly EndDate, string LeaveType, string Status);

// ---- Empleado (CRUD, ámbito sucursal activa) ----
public sealed record SaveEmployeeCommand(
    int? Id, int DepartmentId, int RoleId,
    string FirstName, string LastName, string Email, string Phone, DateOnly HireDate);

public sealed class SaveEmployeeValidator : AbstractValidator<SaveEmployeeCommand>
{
    public SaveEmployeeValidator()
    {
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public sealed class SaveEmployeeHandler(
    IEmployeeRepository employees,
    IDepartmentRepository departments,
    IRoleRepository roles,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<SaveEmployeeCommand> validator)
{
    public async Task<int> HandleAsync(SaveEmployeeCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        var branchId = branchContext.BranchId;

        if (!await departments.ExistsInBranchAsync(command.DepartmentId, branchId, ct))
        {
            throw new NotFoundException("departamento en la sucursal", command.DepartmentId);
        }
        if (!await roles.ExistsAsync(command.RoleId, ct))
        {
            throw new NotFoundException("rol", command.RoleId);
        }

        Employee entity;
        if (command.Id is { } id)
        {
            entity = await employees.GetAsync(id, ct) ?? throw new NotFoundException("empleado", id);
            if (entity.BranchId != branchId)
            {
                throw new BranchAccessDeniedException(entity.BranchId);
            }
            entity.UpdateDetails(command.FirstName, command.LastName, command.Email, command.Phone);
            entity.Reassign(command.DepartmentId, command.RoleId);
        }
        else
        {
            entity = new Employee(branchId, command.DepartmentId, command.RoleId,
                command.FirstName, command.LastName, command.Email, command.Phone, command.HireDate);
            employees.Add(entity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed record ListEmployeesQuery(string? Search, int Page = 1, int PageSize = 20);

public sealed class ListEmployeesHandler(IEmployeeRepository employees, IBranchContext branchContext)
{
    public async Task<PagedResult<EmployeeDto>> HandleAsync(ListEmployeesQuery query, CancellationToken ct = default)
    {
        var branchId = branchContext.BranchId;
        var page = new PageRequest(query.Page, query.PageSize);
        var items = await employees.ListByBranchAsync(branchId, query.Search, page.Skip, page.Take, ct);
        var total = await employees.CountByBranchAsync(branchId, query.Search, ct);
        return new PagedResult<EmployeeDto>(items.Select(Map).ToList(), query.Page, page.Take, total);
    }

    internal static EmployeeDto Map(Employee e) => new(
        e.Id, e.BranchId, e.DepartmentId, e.RoleId, e.FirstName, e.LastName, e.Email, e.Phone, e.HireDate, e.UserId);
}

public sealed class GetEmployeeHandler(IEmployeeRepository employees, IBranchContext branchContext)
{
    public async Task<EmployeeDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var e = await employees.GetAsync(id, ct) ?? throw new NotFoundException("empleado", id);
        if (e.BranchId != branchContext.BranchId)
        {
            throw new BranchAccessDeniedException(e.BranchId);
        }
        return ListEmployeesHandler.Map(e);
    }
}

// ---- Vínculo con el login (Auth API) ----
public sealed class LinkEmployeeUserHandler(IEmployeeRepository employees, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(int employeeId, int userId, CancellationToken ct = default)
    {
        var e = await employees.GetAsync(employeeId, ct) ?? throw new NotFoundException("empleado", employeeId);
        e.LinkUser(userId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class UnlinkEmployeeUserHandler(IEmployeeRepository employees, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(int employeeId, CancellationToken ct = default)
    {
        var e = await employees.GetAsync(employeeId, ct) ?? throw new NotFoundException("empleado", employeeId);
        e.UnlinkUser();
        await unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>Consulta usada solo por la llamada interna servicio-a-servicio desde la Auth API
/// (ver Endpoints/InternalEndpoints.cs) para validar un EmployeeId al crear un login. No aplica
/// scoping de sucursal: a diferencia de GetEmployeeHandler, esta llamada no tiene un usuario
/// logueado ni un branch activo detrás.</summary>
public sealed class EmployeeExistsHandler(IEmployeeRepository employees)
{
    public async Task<bool> HandleAsync(int employeeId, CancellationToken ct = default)
        => await employees.GetAsync(employeeId, ct) is not null;
}

// ---- Turnos (sub-recurso del empleado) ----
public sealed record AddShiftCommand(int EmployeeId, DateTime StartTime, DateTime EndTime, decimal ScheduledHours);

public sealed class AddShiftValidator : AbstractValidator<AddShiftCommand>
{
    public AddShiftValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.ScheduledHours).GreaterThan(0);
    }
}

public sealed class EmployeeShiftsHandler(
    IShiftRepository shifts,
    IEmployeeRepository employees,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<AddShiftCommand> validator)
{
    public async Task<IReadOnlyList<ShiftDto>> ListAsync(int employeeId, CancellationToken ct = default)
    {
        await EnsureEmployeeInBranch(employeeId, ct);
        var rows = await shifts.ListByEmployeeAsync(employeeId, ct);
        return rows.Select(s => new ShiftDto(s.Id, s.EmployeeId, s.StartTime, s.EndTime, s.ScheduledHours)).ToList();
    }

    public async Task<int> AddAsync(AddShiftCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        await EnsureEmployeeInBranch(command.EmployeeId, ct);

        var shift = Shift.Create(command.EmployeeId, command.StartTime, command.EndTime, command.ScheduledHours);
        shifts.Add(shift);
        await unitOfWork.SaveChangesAsync(ct);
        return shift.Id;
    }

    public async Task RemoveAsync(int employeeId, int shiftId, CancellationToken ct = default)
    {
        await EnsureEmployeeInBranch(employeeId, ct);
        var shift = await shifts.GetAsync(shiftId, ct);
        if (shift is null || shift.EmployeeId != employeeId)
        {
            throw new NotFoundException("turno", shiftId);
        }
        shifts.Remove(shift);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EnsureEmployeeInBranch(int employeeId, CancellationToken ct)
    {
        var e = await employees.GetAsync(employeeId, ct) ?? throw new NotFoundException("empleado", employeeId);
        if (e.BranchId != branchContext.BranchId)
        {
            throw new BranchAccessDeniedException(e.BranchId);
        }
    }
}

// ---- Ausencias (sub-recurso del empleado) ----
public sealed record RequestLeaveCommand(int EmployeeId, DateOnly StartDate, DateOnly EndDate, string LeaveType);

public sealed class RequestLeaveValidator : AbstractValidator<RequestLeaveCommand>
{
    public RequestLeaveValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.LeaveType).NotEmpty();
    }
}

public sealed class EmployeeLeavesHandler(
    IEmployeeLeaveRepository leaves,
    IEmployeeRepository employees,
    IBranchContext branchContext,
    IUnitOfWork unitOfWork,
    IValidator<RequestLeaveCommand> validator)
{
    public async Task<IReadOnlyList<LeaveDto>> ListAsync(int employeeId, CancellationToken ct = default)
    {
        await EnsureEmployeeInBranch(employeeId, ct);
        var rows = await leaves.ListByEmployeeAsync(employeeId, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<int> RequestAsync(RequestLeaveCommand command, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(command, ct);
        await EnsureEmployeeInBranch(command.EmployeeId, ct);

        var leave = EmployeeLeave.Request(command.EmployeeId, command.StartDate, command.EndDate, command.LeaveType);
        leaves.Add(leave);
        await unitOfWork.SaveChangesAsync(ct);
        return leave.Id;
    }

    public async Task SetStatusAsync(int employeeId, int leaveId, string action, CancellationToken ct = default)
    {
        await EnsureEmployeeInBranch(employeeId, ct);
        var leave = await leaves.GetAsync(leaveId, ct);
        if (leave is null || leave.EmployeeId != employeeId)
        {
            throw new NotFoundException("ausencia", leaveId);
        }

        switch (action)
        {
            case "approve": leave.Approve(); break;
            case "reject": leave.Reject(); break;
            case "cancel": leave.Cancel(); break;
            default: throw new DomainRuleException("staffing.unknown_action", $"Acción desconocida '{action}'.");
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static LeaveDto Map(EmployeeLeave l) =>
        new(l.Id, l.EmployeeId, l.StartDate, l.EndDate, l.LeaveType, LeaveCatalog.StatusToDb(l.Status));

    private async Task EnsureEmployeeInBranch(int employeeId, CancellationToken ct)
    {
        var e = await employees.GetAsync(employeeId, ct) ?? throw new NotFoundException("empleado", employeeId);
        if (e.BranchId != branchContext.BranchId)
        {
            throw new BranchAccessDeniedException(e.BranchId);
        }
    }
}
