using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Sales.Discounts;
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

        // ---- Descuentos aplicados al pedido ----
        group.MapPost("/orders/{id:int}/discounts", async (
            int id, ApplyOrderDiscountRequest b, ApplyOrderDiscountHandler h, CancellationToken ct) =>
        {
            var rowId = await h.HandleAsync(new ApplyOrderDiscountCommand(id, b.DiscountId), ct);
            return Results.Created($"/api/v1/orders/{id}/discounts/{rowId}", new CreatedIdResponse(rowId));
        });

        group.MapDelete("/orders/{id:int}/discounts/{discountId:int}", async (
            int id, int discountId, RemoveOrderDiscountHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new RemoveOrderDiscountCommand(id, discountId), ct);
            return Results.NoContent();
        });

        // ---- Pagos ----
        group.MapPost("/orders/{id:int}/payments", async (
            int id, RegisterPaymentRequest b, RegisterPaymentHandler h, CancellationToken ct) =>
        {
            var paymentId = await h.HandleAsync(
                new RegisterPaymentCommand(id, b.Method, b.Amount, b.GiftCardId), ct);
            return Results.Created($"/api/v1/orders/{id}/payments/{paymentId}", new CreatedIdResponse(paymentId));
        });

        // ---- Cierre / cancelación ----
        group.MapPost("/orders/{id:int}/close", async (int id, CloseOrderHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/orders/{id:int}/cancel", async (int id, CancelOrderHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(id, ct);
            return Results.NoContent();
        });

        // ---- Catálogo de descuentos ----
        var discounts = app.MapGroup("/api/v1/discounts").WithTags("Ventas").RequireAuthorization("DiscountAccess");

        discounts.MapGet("/", async (
            string? search, int? page, int? pageSize, ListDiscountsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListDiscountsQuery(search, page ?? 1, pageSize ?? 20), ct)));

        discounts.MapGet("/{id:int}", async (int id, GetDiscountHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        discounts.MapPost("/", async (SaveDiscountRequest b, SaveDiscountHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(
                new SaveDiscountCommand(null, b.Name, b.Type, b.Value, b.StartDate, b.EndDate), ct);
            return Results.Created($"/api/v1/discounts/{id}", new CreatedIdResponse(id));
        });

        discounts.MapPut("/{id:int}", async (int id, SaveDiscountRequest b, SaveDiscountHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveDiscountCommand(id, b.Name, b.Type, b.Value, b.StartDate, b.EndDate), ct);
            return Results.NoContent();
        });
    }
}
