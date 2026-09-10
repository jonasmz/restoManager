namespace RestoManager.Business.Api.Contracts;

// Fase 8 — clientes y fidelización. Reseñas: sucursal = sucursal activa (X-Branch-Id).

public sealed record IssueGiftCardRequest(
    int CustomerId, string CardNumber, decimal InitialBalance, DateOnly ExpiryDate);

public sealed record RedeemLoyaltyPointsRequest(int OrderId, int Points);

public sealed record CreateReviewRequest(int CustomerId, int Rating, string? Comment);
