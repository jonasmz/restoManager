using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Deliveries;

namespace RestoManager.Business.Api.Endpoints;

public static class DeliveryEndpoints
{
    public static void MapDeliveryEndpoints(this WebApplication app)
    {
        // ---- Repartidores (catálogo; gestión de personal) ----
        var drivers = app.MapGroup("/api/v1/delivery-drivers").WithTags("Delivery").RequireAuthorization("OrgStaff");

        drivers.MapGet("/", async (
            string? search, int? page, int? pageSize, ListDriversHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListDriversQuery(search, page ?? 1, pageSize ?? 20), ct)));

        drivers.MapGet("/{id:int}", async (int id, GetDriverHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        drivers.MapPost("/", async (SaveDriverRequest b, SaveDriverHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveDriverCommand(null, b.EmployeeId, b.VehicleType, b.LicensePlate), ct);
            return Results.Created($"/api/v1/delivery-drivers/{id}", new CreatedIdResponse(id));
        });

        drivers.MapPut("/{id:int}", async (int id, SaveDriverRequest b, SaveDriverHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveDriverCommand(id, b.EmployeeId, b.VehicleType, b.LicensePlate), ct);
            return Results.NoContent();
        });

        // ---- Entregas (despacho; sucursal activa) ----
        var deliveries = app.MapGroup("/api/v1/deliveries").WithTags("Delivery").RequireAuthorization("DeliveryAccess");

        deliveries.MapGet("/", async (string? status, ListDeliveriesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListDeliveriesQuery(status), ct)));

        deliveries.MapGet("/{id:int}", async (int id, GetDeliveryHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        deliveries.MapPost("/{id:int}/assign", async (
            int id, AssignDriverRequest b, AssignDriverHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new AssignDriverCommand(id, b.DriverId), ct);
            return Results.NoContent();
        });

        deliveries.MapPost("/{id:int}/in-transit", async (int id, AdvanceDeliveryHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, AdvanceDeliveryHandler.Step.InTransit, ct);
            return Results.NoContent();
        });

        deliveries.MapPost("/{id:int}/delivered", async (int id, AdvanceDeliveryHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, AdvanceDeliveryHandler.Step.Delivered, ct);
            return Results.NoContent();
        });

        deliveries.MapPost("/{id:int}/failed", async (int id, AdvanceDeliveryHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, AdvanceDeliveryHandler.Step.Failed, ct);
            return Results.NoContent();
        });

        deliveries.MapPost("/{id:int}/cancel", async (int id, CancelDeliveryHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, ct);
            return Results.NoContent();
        });
    }
}
