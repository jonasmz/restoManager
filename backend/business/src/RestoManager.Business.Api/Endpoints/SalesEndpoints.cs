using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Sales.Orders;

namespace RestoManager.Business.Api.Endpoints;

public static class SalesEndpoints
{
    public static void MapSalesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Ventas").RequireAuthorization("SalesAccess");

        // ---- Pedidos (sucursal activa) ----
        group.MapGet("/orders", async (
            string? channel, string? status, int? sessionId, DateOnly? date, int? page, int? pageSize,
            ListOrdersHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(
                new ListOrdersQuery(channel, status, sessionId, date, page ?? 1, pageSize ?? 20), ct)));

        group.MapGet("/orders/{id:int}", async (int id, GetOrderHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        group.MapPost("/orders", async (CreateOrderRequest b, CreateOrderHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(
                new CreateOrderCommand(b.Channel, b.TableId, b.TableSessionId, b.CustomerId), ct);
            return Results.Created($"/api/v1/orders/{id}", new CreatedIdResponse(id));
        });

        // ---- Ítems del pedido ----
        group.MapPost("/orders/{id:int}/items", async (
            int id, AddOrderItemRequest b, AddOrderItemHandler h, CancellationToken ct) =>
        {
            var itemId = await h.HandleAsync(
                new AddOrderItemCommand(id, b.MenuItemId, b.Quantity, b.Notes), ct);
            return Results.Created($"/api/v1/orders/{id}/items/{itemId}", new CreatedIdResponse(itemId));
        });

        group.MapPut("/orders/{id:int}/items/{itemId:int}", async (
            int id, int itemId, UpdateOrderItemRequest b, UpdateOrderItemHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new UpdateOrderItemCommand(id, itemId, b.Quantity, b.Notes), ct);
            return Results.NoContent();
        });

        group.MapDelete("/orders/{id:int}/items/{itemId:int}", async (
            int id, int itemId, RemoveOrderItemHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new RemoveOrderItemCommand(id, itemId), ct);
            return Results.NoContent();
        });
    }
}
