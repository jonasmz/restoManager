namespace RestoManager.Business.Api.Contracts;

// Sucursal = sucursal activa (header X-Branch-Id). El empleado es el del token.

public sealed record CreateOrderRequest(
    string Channel, int? TableId, int? TableSessionId, int? CustomerId,
    string? DeliveryAddress = null, DateTime? EstimatedTime = null, int? DriverId = null);

public sealed record AddOrderItemRequest(int MenuItemId, int Quantity, string? Notes);

public sealed record UpdateOrderItemRequest(int Quantity, string? Notes);

// ---- Fase 6b ----

public sealed record ApplyOrderDiscountRequest(int DiscountId);

public sealed record RegisterPaymentRequest(string Method, decimal Amount, int? GiftCardId);

public sealed record SaveDiscountRequest(
    string Name, string Type, decimal Value, DateOnly StartDate, DateOnly EndDate);
