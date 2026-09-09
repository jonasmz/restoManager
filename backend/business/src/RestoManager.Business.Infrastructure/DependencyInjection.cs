using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Business.Domain.Abstractions;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Purchasing;
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

        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IBranchInventoryRepository, BranchInventoryRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
        services.AddScoped<IWasteLogRepository, WasteLogRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IEmployeeLeaveRepository, EmployeeLeaveRepository>();

        return services;
    }
}
