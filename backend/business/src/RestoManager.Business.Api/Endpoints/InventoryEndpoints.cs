using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Inventory.Adjustments;
using RestoManager.Business.Application.Inventory.Ingredients;
using RestoManager.Business.Application.Inventory.Stock;
using RestoManager.Business.Application.Inventory.Waste;

namespace RestoManager.Business.Api.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Inventario").RequireAuthorization("InventoryAccess");

        // ---- Ingredientes (catálogo global) ----
        group.MapGet("/ingredients", async (
            string? search, int? page, int? pageSize, ListIngredientsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListIngredientsQuery(search, page ?? 1, pageSize ?? 20), ct)));

        group.MapGet("/ingredients/{id:int}", async (int id, GetIngredientHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(id, ct)));

        group.MapPost("/ingredients", async (
            CreateIngredientRequest body, CreateIngredientHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(new CreateIngredientCommand(body.Name, body.Unit, body.UnitPrice, body.ReorderPoint), ct);
            return Results.Created($"/api/v1/ingredients/{id}", new CreatedIdResponse(id));
        });

        group.MapPut("/ingredients/{id:int}", async (
            int id, UpdateIngredientRequest body, UpdateIngredientHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new UpdateIngredientCommand(id, body.Name, body.Unit, body.UnitPrice, body.ReorderPoint), ct);
            return Results.NoContent();
        });

        // ---- Saldo y movimientos (sucursal activa via X-Branch-Id) ----
        group.MapGet("/inventory", async (GetBranchStockHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)));

        group.MapGet("/inventory/movements", async (
            int? ingredientId, DateTime? from, DateTime? to, string? type,
            ListMovementsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListMovementsQuery(ingredientId, from, to, type), ct)));

        // ---- Mermas ----
        group.MapGet("/inventory/waste", async (
            int? ingredientId, ListWasteLogsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListWasteLogsQuery(ingredientId), ct)));

        group.MapPost("/inventory/waste", async (
            RegisterWasteRequest body, RegisterWasteHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(
                new RegisterWasteCommand(body.IngredientId, body.Quantity, body.Reason), ct);
            return Results.Created($"/api/v1/inventory/waste/{id}", new CreatedIdResponse(id));
        });

        // ---- Ajustes y carga inicial ----
        group.MapPost("/inventory/adjustments", async (
            AdjustStockRequest body, AdjustStockHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new AdjustStockCommand(body.IngredientId, body.Quantity, body.Reason), ct);
            return Results.NoContent();
        });

        group.MapPost("/inventory/initial-load", async (
            LoadInitialStockRequest body, LoadInitialStockHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new LoadInitialStockCommand(body.IngredientId, body.Quantity), ct);
            return Results.NoContent();
        });
    }
}
