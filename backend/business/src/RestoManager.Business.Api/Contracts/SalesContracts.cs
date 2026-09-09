namespace RestoManager.Business.Api.Contracts;

// Sucursal = sucursal activa (header X-Branch-Id). El empleado es el del token.

public sealed record CreateOrderRequest(string Channel, int? TableId, int? TableSessionId, int? CustomerId);

public sealed record AddOrderItemRequest(int MenuItemId, int Quantity, string? Notes);

public sealed record UpdateOrderItemRequest(int Quantity, string? Notes);
