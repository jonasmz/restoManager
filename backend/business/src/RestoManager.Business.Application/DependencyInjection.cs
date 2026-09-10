using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Business.Application.Common;
using RestoManager.Business.Application.Customers;
using RestoManager.Business.Application.Deliveries;
using RestoManager.Business.Application.DiningRoom.Floor;
using RestoManager.Business.Application.DiningRoom.Reservations;
using RestoManager.Business.Application.DiningRoom.Sessions;
using RestoManager.Business.Application.DiningRoom.Tables;
using RestoManager.Business.Application.Inventory.Adjustments;
using RestoManager.Business.Application.Inventory.Ingredients;
using RestoManager.Business.Application.Inventory.Ledger;
using RestoManager.Business.Application.Inventory.Stock;
using RestoManager.Business.Application.Inventory.Waste;
using RestoManager.Business.Application.Menu.Categories;
using RestoManager.Business.Application.Menu.Items;
using RestoManager.Business.Application.Menu.Stations;
using RestoManager.Business.Application.Organization.Branches;
using RestoManager.Business.Application.Organization.Departments;
using RestoManager.Business.Application.Organization.Employees;
using RestoManager.Business.Application.Organization.Restaurants;
using RestoManager.Business.Application.Organization.Roles;
using RestoManager.Business.Application.Purchasing.Orders;
using RestoManager.Business.Application.Purchasing.Suppliers;
using RestoManager.Business.Application.Sales.Consumption;
using RestoManager.Business.Application.Sales.Discounts;
using RestoManager.Business.Application.Sales.Orders;
using RestoManager.Business.Application.Tax.TaxRates;

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

        // Salón (Fase 5)
        services.AddScoped<SaveTableHandler>();
        services.AddScoped<SetTableStatusHandler>();
        services.AddScoped<ListTablesHandler>();
        services.AddScoped<GetTableHandler>();
        services.AddScoped<OpenSessionHandler>();
        services.AddScoped<CloseSessionHandler>();
        services.AddScoped<GetFloorHandler>();
        services.AddScoped<CreateReservationHandler>();
        services.AddScoped<ConfirmReservationHandler>();
        services.AddScoped<CancelReservationHandler>();
        services.AddScoped<MarkNoShowHandler>();
        services.AddScoped<SeatReservationHandler>();
        services.AddScoped<ListReservationsHandler>();
        services.AddScoped<GetReservationHandler>();

        // Ventas (Fase 6)
        services.AddScoped<SaleConsumptionService>();
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<AddOrderItemHandler>();
        services.AddScoped<UpdateOrderItemHandler>();
        services.AddScoped<RemoveOrderItemHandler>();
        services.AddScoped<ListOrdersHandler>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<ApplyOrderDiscountHandler>();
        services.AddScoped<RemoveOrderDiscountHandler>();
        services.AddScoped<RegisterPaymentHandler>();
        services.AddScoped<CloseOrderHandler>();
        services.AddScoped<CancelOrderHandler>();
        services.AddScoped<SaveDiscountHandler>();
        services.AddScoped<ListDiscountsHandler>();
        services.AddScoped<GetDiscountHandler>();

        // Delivery (Fase 7)
        services.AddScoped<SaveDriverHandler>();
        services.AddScoped<ListDriversHandler>();
        services.AddScoped<GetDriverHandler>();
        services.AddScoped<ListDeliveriesHandler>();
        services.AddScoped<GetDeliveryHandler>();
        services.AddScoped<AssignDriverHandler>();
        services.AddScoped<AdvanceDeliveryHandler>();
        services.AddScoped<CancelDeliveryHandler>();

        // Clientes y fidelización (Fase 8)
        services.AddScoped<SaveCustomerHandler>();
        services.AddScoped<ListCustomersHandler>();
        services.AddScoped<GetCustomerHandler>();
        services.AddScoped<ListCustomerOrdersHandler>();
        services.AddScoped<GetCustomerLoyaltyHandler>();
        services.AddScoped<RedeemLoyaltyPointsHandler>();
        services.AddScoped<IssueGiftCardHandler>();
        services.AddScoped<GetGiftCardBalanceHandler>();
        services.AddScoped<ListCustomerGiftCardsHandler>();
        services.AddScoped<CreateReviewHandler>();
        services.AddScoped<ListReviewsHandler>();

        // Organización
        services.AddScoped<SaveRestaurantHandler>();
        services.AddScoped<ListRestaurantsHandler>();
        services.AddScoped<GetRestaurantHandler>();
        services.AddScoped<SaveBranchHandler>();
        services.AddScoped<ListBranchesHandler>();
        services.AddScoped<GetBranchHandler>();
        services.AddScoped<SetBranchPublicSlugHandler>();
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

        // Menú y cocina
        services.AddScoped<SaveCategoryHandler>();
        services.AddScoped<ListCategoriesHandler>();
        services.AddScoped<GetCategoryHandler>();
        services.AddScoped<SaveMenuItemHandler>();
        services.AddScoped<ListMenuItemsHandler>();
        services.AddScoped<GetMenuItemHandler>();
        services.AddScoped<GetMenuItemCostHandler>();
        services.AddScoped<GetMenuItemAvailabilityHandler>();
        services.AddScoped<SetMenuItemAvailabilityHandler>();
        services.AddScoped<SetMenuItemImageHandler>();
        services.AddScoped<ClearMenuItemImageHandler>();
        services.AddScoped<SaveKitchenStationHandler>();
        services.AddScoped<ListKitchenStationsHandler>();
        services.AddScoped<GetKitchenStationHandler>();
        services.AddScoped<SetStationMenuItemsHandler>();

        // Fiscal
        services.AddScoped<SaveTaxRateHandler>();
        services.AddScoped<ListTaxRatesHandler>();
        services.AddScoped<GetTaxRateHandler>();

        // Reportes (Fase 9/10)
        services.AddScoped<Reports.ReportDocumentBuilder>();

        // Carta pública / QR (Fase 11)
        services.AddScoped<PublicCatalog.GetPublicCatalogHandler>();

        return services;
    }
}
