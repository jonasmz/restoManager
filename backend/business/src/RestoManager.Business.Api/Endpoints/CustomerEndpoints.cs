using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Customers;

namespace RestoManager.Business.Api.Endpoints;

/// <summary>
/// Alta rápida de clientes (Fase 5, para reservas). Catálogo global. La Fase 8 se
/// hará dueña del módulo y ampliará fidelización, gift cards y reseñas.
/// </summary>
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Clientes").RequireAuthorization("DiningRoomAccess");

        group.MapGet("/customers", async (
            string? search, int? page, int? pageSize, ListCustomersHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListCustomersQuery(search, page ?? 1, pageSize ?? 20), ct)));

        group.MapGet("/customers/{id:int}", async (int id, GetCustomerHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        group.MapPost("/customers", async (SaveCustomerRequest b, SaveCustomerHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveCustomerCommand(null, b.FirstName, b.LastName, b.Phone, b.Email), ct);
            return Results.Created($"/api/v1/customers/{id}", new CreatedIdResponse(id));
        });

        group.MapPut("/customers/{id:int}", async (
            int id, SaveCustomerRequest b, SaveCustomerHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveCustomerCommand(id, b.FirstName, b.LastName, b.Phone, b.Email), ct);
            return Results.NoContent();
        });
    }
}
