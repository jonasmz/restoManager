using Microsoft.Extensions.Configuration;
using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Organization.Employees;

namespace RestoManager.Business.Api.Endpoints;

/// <summary>Endpoints servicio-a-servicio, llamados solo por la Auth API para validar/vincular
/// un EmployeeId al crear un login (ver docs de la vinculación Usuario↔Empleado). No pasan por
/// nginx (que no enruta /internal/) ni por el JWT de usuario: se protegen con una clave
/// compartida (Internal:ApiKey, ver deploy/.env.example) en el header X-Internal-Key.</summary>
public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/internal")
            .WithTags("Interno")
            .AddEndpointFilter<InternalApiKeyFilter>();

        group.MapGet("/employees/{id:int}", async (int id, EmployeeExistsHandler h, CancellationToken ct) =>
            Results.Ok(new { exists = await h.HandleAsync(id, ct) }));

        group.MapPut("/employees/{id:int}/user-id", async (int id, LinkEmployeeUserRequest b, LinkEmployeeUserHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, b.UserId, ct);
            return Results.NoContent();
        });
    }
}

internal sealed class InternalApiKeyFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["Internal:ApiKey"];
        if (string.IsNullOrEmpty(expected))
        {
            return Results.Problem("El acceso interno no está configurado.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var provided = context.HttpContext.Request.Headers["X-Internal-Key"].ToString();
        if (!string.Equals(provided, expected, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
