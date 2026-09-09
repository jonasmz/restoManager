namespace RestoManager.Business.Api.Contracts;

public sealed record CreateIngredientRequest(string Name, string Unit, decimal UnitPrice);
public sealed record UpdateIngredientRequest(string Name, string Unit, decimal UnitPrice);

public sealed record RegisterWasteRequest(int BranchId, int IngredientId, decimal Quantity, string Reason);
public sealed record AdjustStockRequest(int BranchId, int IngredientId, decimal Quantity, string Reason);
public sealed record LoadInitialStockRequest(int BranchId, int IngredientId, decimal Quantity);

public sealed record CreatedIdResponse(int Id);
