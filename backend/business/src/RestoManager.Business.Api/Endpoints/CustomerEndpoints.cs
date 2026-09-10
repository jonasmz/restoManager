using RestoManager.Business.Api.Contracts;
using RestoManager.Business.Application.Customers;

namespace RestoManager.Business.Api.Endpoints;

/// <summary>
/// Clientes y fidelización (Fase 8): CRUD de clientes, historial de pedidos, saldo y
/// canje de puntos, emisión y consulta de gift cards, y reseñas por sucursal.
/// Catálogo de clientes global; reseñas por <c>X-Branch-Id</c>. Política
/// <c>DiningRoomAccess</c> (ADMIN, BRANCH_MANAGER, WAITER) — el POS necesita leer y
/// identificar clientes.
/// </summary>
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Clientes").RequireAuthorization("DiningRoomAccess");

        // ---- Clientes ----

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

        // ---- Historial de pedidos y fidelización ----

        group.MapGet("/customers/{id:int}/orders", async (
            int id, int? page, int? pageSize, ListCustomerOrdersHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListCustomerOrdersQuery(id, page ?? 1, pageSize ?? 20), ct)));

        group.MapGet("/customers/{id:int}/loyalty", async (
            int id, GetCustomerLoyaltyHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        group.MapPost("/customers/{id:int}/loyalty/redeem", async (
            int id, RedeemLoyaltyPointsRequest b, RedeemLoyaltyPointsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new RedeemLoyaltyPointsCommand(id, b.OrderId, b.Points), ct)));

        // ---- Gift cards ----

        group.MapGet("/customers/{id:int}/gift-cards", async (
            int id, ListCustomerGiftCardsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(id, ct)));

        group.MapPost("/gift-cards", async (IssueGiftCardRequest b, IssueGiftCardHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(
                new IssueGiftCardCommand(b.CustomerId, b.CardNumber, b.InitialBalance, b.ExpiryDate), ct);
            return Results.Created($"/api/v1/gift-cards/{b.CardNumber}/balance", new CreatedIdResponse(id));
        });

        group.MapGet("/gift-cards/{cardNumber}/balance", async (
            string cardNumber, GetGiftCardBalanceHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(cardNumber, ct)));

        // ---- Reseñas (sucursal activa) ----

        group.MapGet("/reviews", async (
            int? minRating, int? page, int? pageSize, ListReviewsHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(new ListReviewsQuery(minRating, page ?? 1, pageSize ?? 20), ct)));

        group.MapPost("/reviews", async (CreateReviewRequest b, CreateReviewHandler h, CancellationToken ct) =>
        {
            var id = await h.HandleAsync(new CreateReviewCommand(b.CustomerId, b.Rating, b.Comment), ct);
            return Results.Created($"/api/v1/reviews/{id}", new CreatedIdResponse(id));
        });
    }
}
