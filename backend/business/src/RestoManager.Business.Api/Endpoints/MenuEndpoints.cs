using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Menu.Categories;
using RestoManager.Business.Application.Menu.Items;
using RestoManager.Business.Application.Menu.Stations;
using RestoManager.Business.Application.Tax.TaxRates;

namespace RestoManager.Business.Api.Endpoints;

public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this WebApplication app)
    {
        var menu = app.MapGroup("/api/v1").WithTags("Menú").RequireAuthorization("MenuAccess");

        // ---- Categorías (catálogo global) ----
        menu.MapGet("/categories", async (
            string? search, int? page, int? pageSize, ListCategoriesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListCategoriesQuery(search, page ?? 1, pageSize ?? 20), ct)));

        menu.MapGet("/categories/{id:int}", async (int id, GetCategoryHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        menu.MapPost("/categories", async (SaveCategoryRequest b, SaveCategoryHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveCategoryCommand(null, b.Name, b.Description), ct);
            return Results.Created($"/api/v1/categories/{id}", new CreatedIdResponse(id));
        });

        menu.MapPut("/categories/{id:int}", async (int id, SaveCategoryRequest b, SaveCategoryHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveCategoryCommand(id, b.Name, b.Description), ct);
            return Results.NoContent();
        });

        // ---- Platos (catálogo global; receta e impuestos en el mismo cuerpo) ----
        menu.MapGet("/menu-items", async (
            int? categoryId, string? search, int? page, int? pageSize, ListMenuItemsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListMenuItemsQuery(categoryId, search, page ?? 1, pageSize ?? 20), ct)));

        menu.MapGet("/menu-items/{id:int}", async (int id, GetMenuItemHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        menu.MapGet("/menu-items/{id:int}/recipe", async (int id, GetMenuItemHandler h, CancellationToken ct) =>
            Results.Ok((await h.HandleAsync(id, ct)).Recipe));

        menu.MapGet("/menu-items/{id:int}/taxes", async (int id, GetMenuItemHandler h, CancellationToken ct) =>
            Results.Ok((await h.HandleAsync(id, ct)).TaxRateIds));

        menu.MapGet("/menu-items/{id:int}/cost", async (int id, GetMenuItemCostHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        menu.MapPost("/menu-items", async (SaveMenuItemRequest b, SaveMenuItemHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(ToCommand(null, b), ct);
            return Results.Created($"/api/v1/menu-items/{id}", new CreatedIdResponse(id));
        });

        menu.MapPut("/menu-items/{id:int}", async (int id, SaveMenuItemRequest b, SaveMenuItemHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(ToCommand(id, b), ct);
            return Results.NoContent();
        });

        // Disponibilidad por sucursal activa (X-Branch-Id).
        menu.MapGet("/menu-items/{id:int}/availability", async (
            int id, GetMenuItemAvailabilityHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        menu.MapPut("/menu-items/{id:int}/availability", async (
            int id, SetAvailabilityRequest b, SetMenuItemAvailabilityHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SetMenuItemAvailabilityCommand(id, b.IsAvailable), ct);
            return Results.NoContent();
        });

        // ---- Estaciones de cocina (sucursal activa) ----
        menu.MapGet("/kitchen-stations", async (ListKitchenStationsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(ct)));

        menu.MapGet("/kitchen-stations/{id:int}", async (int id, GetKitchenStationHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        menu.MapPost("/kitchen-stations", async (
            SaveKitchenStationRequest b, SaveKitchenStationHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveKitchenStationCommand(null, b.Name, b.Description), ct);
            return Results.Created($"/api/v1/kitchen-stations/{id}", new CreatedIdResponse(id));
        });

        menu.MapPut("/kitchen-stations/{id:int}", async (
            int id, SaveKitchenStationRequest b, SaveKitchenStationHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveKitchenStationCommand(id, b.Name, b.Description), ct);
            return Results.NoContent();
        });

        menu.MapPut("/kitchen-stations/{id:int}/menu-items", async (
            int id, SetStationMenuItemsRequest b, SetStationMenuItemsHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SetStationMenuItemsCommand(id, b.MenuItemIds), ct);
            return Results.NoContent();
        });

        // ---- Tasas de impuesto (catálogo global) ----
        menu.MapGet("/tax-rates", async (
            string? search, int? page, int? pageSize, ListTaxRatesHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListTaxRatesQuery(search, page ?? 1, pageSize ?? 20), ct)));

        menu.MapGet("/tax-rates/{id:int}", async (int id, GetTaxRateHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        menu.MapPost("/tax-rates", async (SaveTaxRateRequest b, SaveTaxRateHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new SaveTaxRateCommand(null, b.Name, b.Rate), ct);
            return Results.Created($"/api/v1/tax-rates/{id}", new CreatedIdResponse(id));
        });

        menu.MapPut("/tax-rates/{id:int}", async (int id, SaveTaxRateRequest b, SaveTaxRateHandler h, CancellationToken ct) =>
        {
            await h.HandleAsync(new SaveTaxRateCommand(id, b.Name, b.Rate), ct);
            return Results.NoContent();
        });
    }

    private static SaveMenuItemCommand ToCommand(int? id, SaveMenuItemRequest b) => new(
        id, b.CategoryId, b.Name, b.Description, b.Price, b.IsAvailable,
        b.Recipe?.Select(r => new RecipeLineDto(r.IngredientId, r.QuantityRequired)).ToList(),
        b.TaxRateIds);
}
