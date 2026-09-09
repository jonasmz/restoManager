using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Purchasing.Orders;
using RestoManager.Business.Application.Purchasing.Suppliers;

namespace RestoManager.Business.Api.Endpoints;

public static class PurchasingEndpoints
{
    public static void MapPurchasingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Compras").RequireAuthorization("InventoryAccess");

        // ---- Proveedores ----
        group.MapGet("/suppliers", async (
            string? search, int? page, int? pageSize, ListSuppliersHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(
                new ListSuppliersQuery(search, page ?? 1, pageSize ?? 20), ct)));

        group.MapGet("/suppliers/{id:int}", async (int id, GetSupplierHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(id, ct)));

        group.MapPost("/suppliers", async (SaveSupplierRequest body, SaveSupplierHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(new SaveSupplierCommand(
                null, body.Name, body.ContactName, body.Phone, body.Email, body.Address), ct);
            return Results.Created($"/api/v1/suppliers/{id}", new CreatedIdResponse(id));
        });

        group.MapPut("/suppliers/{id:int}", async (
            int id, SaveSupplierRequest body, SaveSupplierHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new SaveSupplierCommand(
                id, body.Name, body.ContactName, body.Phone, body.Email, body.Address), ct);
            return Results.NoContent();
        });

        // ---- Órdenes de compra (sucursal activa via X-Branch-Id) ----
        group.MapGet("/purchase-orders", async (
            string? status, int? page, int? pageSize,
            ListPurchaseOrdersHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(
                new ListPurchaseOrdersQuery(status, page ?? 1, pageSize ?? 20), ct)));

        group.MapGet("/purchase-orders/{id:int}", async (
            int id, GetPurchaseOrderHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(id, ct)));

        group.MapPost("/purchase-orders", async (
            CreatePurchaseOrderRequest body, CreatePurchaseOrderHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(new CreatePurchaseOrderCommand(
                body.SupplierId, body.OrderDate, body.Items), ct);
            return Results.Created($"/api/v1/purchase-orders/{id}", new CreatedIdResponse(id));
        });

        group.MapPost("/purchase-orders/{id:int}/send", async (
            int id, SendPurchaseOrderHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/purchase-orders/{id:int}/receive", async (
            int id, ReceivePurchaseOrderHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/purchase-orders/{id:int}/cancel", async (
            int id, CancelPurchaseOrderHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(id, ct);
            return Results.NoContent();
        });
    }
}
