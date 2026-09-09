using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.DiningRoom.Floor;
using RestoManager.Business.Application.DiningRoom.Reservations;
using RestoManager.Business.Application.DiningRoom.Sessions;
using RestoManager.Business.Application.DiningRoom.Tables;

namespace RestoManager.Business.Api.Endpoints;

public static class DiningRoomEndpoints
{
    public static void MapDiningRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Salón").RequireAuthorization("DiningRoomAccess");

        // ---- Mesas (sucursal activa) ----
        group.MapGet("/tables", async (ListTablesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));

        group.MapGet("/tables/{id:int}", async (int id, GetTableHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        group.MapPost("/tables", async (SaveTableRequest b, SaveTableHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveTableCommand(null, b.Number, b.Capacity), ct);
            return Results.Created($"/api/v1/tables/{id}", new CreatedIdResponse(id));
        });

        group.MapPut("/tables/{id:int}", async (int id, SaveTableRequest b, SaveTableHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveTableCommand(id, b.Number, b.Capacity), ct);
            return Results.NoContent();
        });

        group.MapPut("/tables/{id:int}/status", async (
            int id, SetTableStatusRequest b, SetTableStatusHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SetTableStatusCommand(id, b.OperationalStatus), ct);
            return Results.NoContent();
        });

        // ---- Sesiones de mesa ----
        group.MapPost("/tables/{id:int}/sessions", async (
            int id, OpenSessionRequest b, OpenSessionHandler h, CancellationToken ct) =>
        {
            var sid = await h.HandleAsync(new OpenSessionCommand(id, b.GuestCount), ct);
            return Results.Created($"/api/v1/tables/{id}/sessions/{sid}", new CreatedIdResponse(sid));
        });

        group.MapPost("/tables/{id:int}/sessions/{sid:int}/close", async (
            int id, int sid, CloseSessionHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new CloseSessionCommand(id, sid), ct);
            return Results.NoContent();
        });

        // ---- Tablero de salón (estado derivado, Anexo B) ----
        group.MapGet("/floor", async (GetFloorHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));

        // ---- Reservas ----
        group.MapGet("/reservations", async (
            DateOnly? date, string? status, ListReservationsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListReservationsQuery(date, status), ct)));

        group.MapGet("/reservations/{id:int}", async (int id, GetReservationHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        group.MapPost("/reservations", async (
            CreateReservationRequest b, CreateReservationHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(
                new CreateReservationCommand(b.CustomerId, b.TableId, b.ReservationTime, b.PartySize), ct);
            return Results.Created($"/api/v1/reservations/{id}", new CreatedIdResponse(id));
        });

        group.MapPost("/reservations/{id:int}/confirm", async (
            int id, ConfirmReservationHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/reservations/{id:int}/cancel", async (
            int id, CancelReservationHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/reservations/{id:int}/no-show", async (
            int id, MarkNoShowHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/reservations/{id:int}/seat", async (
            int id, SeatReservationRequest b, SeatReservationHandler h, CancellationToken ct) =>
        {
            var sid = await h.HandleAsync(new SeatReservationCommand(id, b.GuestCount), ct);
            return Results.Created($"/api/v1/reservations/{id}", new CreatedIdResponse(sid));
        });
    }
}
