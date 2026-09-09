using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.Delivery;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Purchasing;
using RestoManager.Business.Domain.Sales;
using RestoManager.Business.Domain.Tax;

namespace RestoManager.Business.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core de <c>resto_business</c>. Modela las 38 tablas de
/// <c>requirements/restaurant_schema.sql</c>. La nomenclatura snake_case se aplica
/// vía <c>UseSnakeCaseNamingConvention()</c> en la composición.
/// </summary>
public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options) : DbContext(options)
{
    // Organización
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<EmployeeLeave> EmployeeLeaves => Set<EmployeeLeave>();

    // Salón
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<TableSession> TableSessions => Set<TableSession>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    // Menú y cocina
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<KitchenStation> KitchenStations => Set<KitchenStation>();
    public DbSet<StationMenuItem> StationMenuItems => Set<StationMenuItem>();
    public DbSet<MenuItemBranchAvailability> MenuItemBranchAvailabilities => Set<MenuItemBranchAvailability>();

    // Ventas
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<OrderDiscount> OrderDiscounts => Set<OrderDiscount>();

    // Cliente
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<GiftCardTransaction> GiftCardTransactions => Set<GiftCardTransaction>();

    // Inventario
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<BranchInventory> BranchInventories => Set<BranchInventory>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<WasteLog> WasteLogs => Set<WasteLog>();

    // Compras
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    // Delivery
    public DbSet<DeliveryDriver> DeliveryDrivers => Set<DeliveryDriver>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();

    // Fiscal
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<MenuItemTax> MenuItemTaxes => Set<MenuItemTax>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // El DDL usa `timestamp` (sin zona) y dinero decimal(10,2).
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<decimal>().HavePrecision(10, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BusinessDbContext).Assembly);
    }
}
