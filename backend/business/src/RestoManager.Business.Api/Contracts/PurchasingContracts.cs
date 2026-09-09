using RestoManager.Business.Application.Purchasing.Orders;

namespace RestoManager.Business.Api.Contracts;

public sealed record SaveSupplierRequest(
    string Name, string ContactName, string Phone, string Email, string Address);

// Sucursal = sucursal activa (header X-Branch-Id).
public sealed record CreatePurchaseOrderRequest(
    int SupplierId, DateOnly OrderDate, IReadOnlyList<PurchaseOrderLineDto> Items);
