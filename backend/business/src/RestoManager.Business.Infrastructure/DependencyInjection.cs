using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Purchasing;
using RestoManager.Business.Domain.Tax;
using RestoManager.Business.Infrastructure.Persistence;
using RestoManager.Business.Infrastructure.Persistence.Repositories;
using RestoManager.Business.Infrastructure.Setup;
using RestoManager.Business.Infrastructure.Time;

namespace RestoManager.Business.Infrastructure;

/// <summary>Adaptadores de salida de la Business API: persistencia EF Core sobre <c>resto_business</c>.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BusinessDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("BusinessDb"))
            .UseSnakeCaseNamingConvention());

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<BusinessDevSeeder>();

        // TBL-02: ventana que convierte una reserva CONFIRMED en RESERVED (configurable).
        var windowSection = configuration.GetSection("Salon:ReservationWindow");
        services.AddSingleton(ReservationWindow.FromMinutes(
            int.TryParse(windowSection["BeforeMinutes"], out var before) ? before : 30,
            int.TryParse(windowSection["AfterMinutes"], out var after) ? after : 30));

        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IBranchInventoryRepository, BranchInventoryRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
        services.AddScoped<IWasteLogRepository, WasteLogRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<ITableSessionRepository, TableSessionRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IEmployeeLeaveRepository, EmployeeLeaveRepository>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IKitchenStationRepository, KitchenStationRepository>();
        services.AddScoped<IMenuItemAvailabilityRepository, MenuItemAvailabilityRepository>();
        services.AddScoped<ITaxRateRepository, TaxRateRepository>();

        return services;
    }
}
