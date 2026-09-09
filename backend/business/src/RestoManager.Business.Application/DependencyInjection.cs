using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Inventory.Adjustments;
using RestoManager.Business.Application.Inventory.Ingredients;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Application.Inventory.Stock;
using RestoManager.Business.Application.Inventory.Waste;
using RestoManager.Business.Application.Purchasing.Orders;
using RestoManager.Business.Application.Purchasing.Suppliers;

namespace RestoManager.Business.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateIngredientValidator>();

        services.AddScoped<BranchAccessGuard>();
        services.AddScoped<InventoryLedger>();

        // Inventario
        services.AddScoped<CreateIngredientHandler>();
        services.AddScoped<UpdateIngredientHandler>();
        services.AddScoped<ListIngredientsHandler>();
        services.AddScoped<GetIngredientHandler>();
        services.AddScoped<GetBranchStockHandler>();
        services.AddScoped<ListMovementsHandler>();
        services.AddScoped<RegisterWasteHandler>();
        services.AddScoped<ListWasteLogsHandler>();
        services.AddScoped<AdjustStockHandler>();
        services.AddScoped<LoadInitialStockHandler>();

        // Compras
        services.AddScoped<SaveSupplierHandler>();
        services.AddScoped<ListSuppliersHandler>();
        services.AddScoped<GetSupplierHandler>();
        services.AddScoped<CreatePurchaseOrderHandler>();
        services.AddScoped<SendPurchaseOrderHandler>();
        services.AddScoped<ReceivePurchaseOrderHandler>();
        services.AddScoped<CancelPurchaseOrderHandler>();
        services.AddScoped<ListPurchaseOrdersHandler>();
        services.AddScoped<GetPurchaseOrderHandler>();

        return services;
    }
}
