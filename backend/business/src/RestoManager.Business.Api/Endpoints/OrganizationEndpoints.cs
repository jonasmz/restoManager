using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Organization.Branches;
using RestoManager.Business.Application.Organization.Departments;
using RestoManager.Business.Application.Organization.Employees;
using RestoManager.Business.Application.Organization.Restaurants;
using RestoManager.Business.Application.Organization.Roles;

namespace RestoManager.Business.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        // Dos grupos independientes sobre el mismo prefijo: RequireAuthorization en un
        // RouteGroupBuilder muta el grupo, no crea uno nuevo.
        var staff = app.MapGroup("/api/v1").WithTags("Organización").RequireAuthorization("OrgStaff");
        var admin = app.MapGroup("/api/v1").WithTags("Organización").RequireAuthorization("OrgAdmin");

        // ---- Restaurantes (global) ----
        staff.MapGet("/restaurants", async (int? page, int? pageSize, ListRestaurantsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListRestaurantsQuery(page ?? 1, pageSize ?? 20), ct)));
        staff.MapGet("/restaurants/{id:int}", async (int id, GetRestaurantHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));
        admin.MapPost("/restaurants", async (SaveRestaurantRequest b, SaveRestaurantHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveRestaurantCommand(null, b.Name, b.Address, b.Phone, b.Email, b.TaxNumber), ct);
            return Results.Created($"/api/v1/restaurants/{id}", new CreatedIdResponse(id));
        });
        admin.MapPut("/restaurants/{id:int}", async (int id, SaveRestaurantRequest b, SaveRestaurantHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveRestaurantCommand(id, b.Name, b.Address, b.Phone, b.Email, b.TaxNumber), ct);
            return Results.NoContent();
        });

        // ---- Sucursales ----
        staff.MapGet("/branches", async (int? restaurantId, int? page, int? pageSize, ListBranchesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListBranchesQuery(restaurantId, page ?? 1, pageSize ?? 20), ct)));
        staff.MapGet("/branches/{id:int}", async (int id, GetBranchHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));
        admin.MapPost("/branches", async (SaveBranchRequest b, SaveBranchHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveBranchCommand(
                null, b.RestaurantId, b.Name, b.Address, b.Phone, b.Email, b.OpeningTime, b.ClosingTime), ct);
            return Results.Created($"/api/v1/branches/{id}", new CreatedIdResponse(id));
        });
        admin.MapPut("/branches/{id:int}", async (int id, SaveBranchRequest b, SaveBranchHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveBranchCommand(
                id, b.RestaurantId, b.Name, b.Address, b.Phone, b.Email, b.OpeningTime, b.ClosingTime), ct);
            return Results.NoContent();
        });

        // ---- Roles / puestos (global) ----
        staff.MapGet("/roles", async (int? page, int? pageSize, ListRolesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListRolesQuery(page ?? 1, pageSize ?? 20), ct)));
        staff.MapGet("/roles/{id:int}", async (int id, GetRoleHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));
        admin.MapPost("/roles", async (SaveRoleRequest b, SaveRoleHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveRoleCommand(null, b.Name, b.Description, b.HourlyRate), ct);
            return Results.Created($"/api/v1/roles/{id}", new CreatedIdResponse(id));
        });
        admin.MapPut("/roles/{id:int}", async (int id, SaveRoleRequest b, SaveRoleHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveRoleCommand(id, b.Name, b.Description, b.HourlyRate), ct);
            return Results.NoContent();
        });

        // ---- Departamentos (sucursal activa) ----
        staff.MapGet("/departments", async (ListDepartmentsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));
        staff.MapGet("/departments/{id:int}", async (int id, GetDepartmentHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));
        staff.MapPost("/departments", async (SaveDepartmentRequest b, SaveDepartmentHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveDepartmentCommand(null, b.Name, b.Description), ct);
            return Results.Created($"/api/v1/departments/{id}", new CreatedIdResponse(id));
        });
        staff.MapPut("/departments/{id:int}", async (int id, SaveDepartmentRequest b, SaveDepartmentHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveDepartmentCommand(id, b.Name, b.Description), ct);
            return Results.NoContent();
        });

        // ---- Empleados (sucursal activa) ----
        staff.MapGet("/employees", async (string? search, int? page, int? pageSize, ListEmployeesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListEmployeesQuery(search, page ?? 1, pageSize ?? 20), ct)));
        staff.MapGet("/employees/{id:int}", async (int id, GetEmployeeHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));
        staff.MapPost("/employees", async (SaveEmployeeRequest b, SaveEmployeeHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveEmployeeCommand(
                null, b.DepartmentId, b.RoleId, b.FirstName, b.LastName, b.Email, b.Phone, b.HireDate), ct);
            return Results.Created($"/api/v1/employees/{id}", new CreatedIdResponse(id));
        });
        staff.MapPut("/employees/{id:int}", async (int id, SaveEmployeeRequest b, SaveEmployeeHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveEmployeeCommand(
                id, b.DepartmentId, b.RoleId, b.FirstName, b.LastName, b.Email, b.Phone, b.HireDate), ct);
            return Results.NoContent();
        });

        // ---- Turnos ----
        staff.MapGet("/employees/{employeeId:int}/shifts", async (int employeeId, EmployeeShiftsHandler h, CancellationToken ct) =>
            Results.Ok(await h.ListAsync(employeeId, ct)));
        staff.MapPost("/employees/{employeeId:int}/shifts", async (int employeeId, AddShiftRequest b, EmployeeShiftsHandler h, CancellationToken ct) =>
        {
            var id = await h.AddAsync(new AddShiftCommand(employeeId, b.StartTime, b.EndTime, b.ScheduledHours), ct);
            return Results.Created($"/api/v1/employees/{employeeId}/shifts/{id}", new CreatedIdResponse(id));
        });
        staff.MapDelete("/employees/{employeeId:int}/shifts/{shiftId:int}", async (int employeeId, int shiftId, EmployeeShiftsHandler h, CancellationToken ct) =>
        {
            await h.RemoveAsync(employeeId, shiftId, ct);
            return Results.NoContent();
        });

        // ---- Ausencias ----
        staff.MapGet("/employees/{employeeId:int}/leaves", async (int employeeId, EmployeeLeavesHandler h, CancellationToken ct) =>
            Results.Ok(await h.ListAsync(employeeId, ct)));
        staff.MapPost("/employees/{employeeId:int}/leaves", async (int employeeId, RequestLeaveRequest b, EmployeeLeavesHandler h, CancellationToken ct) =>
        {
            var id = await h.RequestAsync(new RequestLeaveCommand(employeeId, b.StartDate, b.EndDate, b.LeaveType), ct);
            return Results.Created($"/api/v1/employees/{employeeId}/leaves/{id}", new CreatedIdResponse(id));
        });
        staff.MapPost("/employees/{employeeId:int}/leaves/{leaveId:int}/{action}", async (
            int employeeId, int leaveId, string action, EmployeeLeavesHandler h, CancellationToken ct) =>
        {
            await h.SetStatusAsync(employeeId, leaveId, action, ct);
            return Results.NoContent();
        });
    }
}
