using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Inventory.Adjustments;
using RestoManager.Business.Application.Inventory.Ingredients;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Application.Inventory.Stock;
using RestoManager.Business.Application.Inventory.Waste;
using RestoManager.Business.Application.Organization.Branches;
using RestoManager.Business.Application.Organization.Departments;
using RestoManager.Business.Application.Organization.Employees;
using RestoManager.Business.Application.Organization.Restaurants;
using RestoManager.Business.Application.Organization.Roles;
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

        // Organización
        services.AddScoped<SaveRestaurantHandler>();
        services.AddScoped<ListRestaurantsHandler>();
        services.AddScoped<GetRestaurantHandler>();
        services.AddScoped<SaveBranchHandler>();
        services.AddScoped<ListBranchesHandler>();
        services.AddScoped<GetBranchHandler>();
        services.AddScoped<SaveRoleHandler>();
        services.AddScoped<ListRolesHandler>();
        services.AddScoped<GetRoleHandler>();
        services.AddScoped<SaveDepartmentHandler>();
        services.AddScoped<ListDepartmentsHandler>();
        services.AddScoped<GetDepartmentHandler>();
        services.AddScoped<SaveEmployeeHandler>();
        services.AddScoped<ListEmployeesHandler>();
        services.AddScoped<GetEmployeeHandler>();
        services.AddScoped<EmployeeShiftsHandler>();
        services.AddScoped<EmployeeLeavesHandler>();

        return services;
    }
}
