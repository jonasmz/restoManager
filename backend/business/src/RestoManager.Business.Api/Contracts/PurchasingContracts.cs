using RestoManager.Business.Application.Purchasing.Orders;

namespace RestoManager.Business.Api.Contracts;

public sealed record SaveSupplierRequest(
    string Name, string ContactName, string Phone, string Email, string Address);

public sealed record CreatePurchaseOrderRequest(
    int SupplierId, int BranchId, DateOnly OrderDate, IReadOnlyList<PurchaseOrderLineDto> Items);
