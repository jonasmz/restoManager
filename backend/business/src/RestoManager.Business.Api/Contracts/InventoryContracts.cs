namespace RestoManager.Business.Api.Contracts;

public sealed record CreateIngredientRequest(string Name, string Unit, decimal UnitPrice, decimal ReorderPoint = 0m);
public sealed record UpdateIngredientRequest(string Name, string Unit, decimal UnitPrice, decimal ReorderPoint = 0m);

// Sucursal = sucursal activa (header X-Branch-Id).
public sealed record RegisterWasteRequest(int IngredientId, decimal Quantity, string Reason);
public sealed record AdjustStockRequest(int IngredientId, decimal Quantity, string Reason);
public sealed record LoadInitialStockRequest(int IngredientId, decimal Quantity);

public sealed record CreatedIdResponse(int Id);
