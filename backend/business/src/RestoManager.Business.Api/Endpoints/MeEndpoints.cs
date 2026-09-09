using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Abstractions;

namespace RestoManager.Business.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Me");

        group.MapGet("/me", (ICurrentUser user) => Results.Ok(new MeResponse(
                user.UserId, user.Email, user.Roles, user.EmployeeId, user.BranchIds)))
            .RequireAuthorization()
            .WithSummary("Devuelve los datos del usuario autenticado (verifica la cadena JWT).");

        // Endpoint de ejemplo para probar autorización por rol.
        group.MapGet("/admin-check", () => Results.Ok(new { ok = true }))
            .RequireAuthorization("RequireAdmin")
            .WithSummary("Solo ADMIN. Sirve para verificar las policies.");
    }
}
