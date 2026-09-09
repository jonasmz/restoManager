using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Sales;
using RestoManager.Business.Domain.Tax;
using RestoManager.Business.Infrastructure.Persistence;

namespace RestoManager.Business.Infrastructure.Setup;

/// <summary>
/// Semilla de desarrollo: 1 empresa, 2 sucursales, departamentos, roles/puestos y
/// empleados demo. El empleado id=1 (sucursal 1) corresponde al claim
/// <c>employee_id</c> del admin de arranque de la Auth API. Cada bloque es
/// idempotente y corre también sobre una BD ya sembrada.
/// La reemplazará el seed real parametrizable cuando se defina.
/// </summary>
public sealed class BusinessDevSeeder(BusinessDbContext db, ILogger<BusinessDevSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedOrganizationAsync(cancellationToken);
        await SeedDiningRoomAsync(cancellationToken);
        await SeedSalesAsync(cancellationToken);
    }

    private async Task SeedOrganizationAsync(CancellationToken cancellationToken)
    {
        if (await db.Branches.AnyAsync(cancellationToken))
        {
            // Organización ya sembrada; asegura la carta demo (Fase 4) de forma idempotente.
            await SeedMenuAsync(cancellationToken);
            return;
        }

        var restaurant = new Restaurant("Resto Manager Demo", "Av. Central 100", "555-0000", "demo@resto.local", "30-00000000-0");
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync(cancellationToken); // restaurant.Id = 1

        var centro = new Branch(restaurant.Id, "Sucursal Centro", "Av. Central 100", "555-0001", "centro@resto.local",
            new TimeOnly(8, 0), new TimeOnly(23, 30));
        var norte = new Branch(restaurant.Id, "Sucursal Norte", "Av. Norte 500", "555-0002", "norte@resto.local",
            new TimeOnly(9, 0), new TimeOnly(23, 0));
        db.Branches.AddRange(centro, norte);
        await db.SaveChangesAsync(cancellationToken); // centro.Id = 1, norte.Id = 2

        var salonCentro = new Department(centro.Id, "Salón", "Atención en salón");
        var cocinaCentro = new Department(centro.Id, "Cocina", "Producción");
        var salonNorte = new Department(norte.Id, "Salón", "Atención en salón");
        db.Departments.AddRange(salonCentro, cocinaCentro, salonNorte);

        var gerente = new Role("Gerente", "Gerencia de sucursal", 0m);
        var mesero = new Role("Mesero", "Atención de mesas", 0m);
        var cocinero = new Role("Cocinero", "Cocina", 0m);
        var cajero = new Role("Cajero", "Caja", 0m);
        var almacenero = new Role("Almacenero", "Depósito e inventario", 0m);
        db.Roles.AddRange(gerente, mesero, cocinero, cajero, almacenero);
        await db.SaveChangesAsync(cancellationToken);

        var admin = new Employee(centro.Id, salonCentro.Id, gerente.Id,
            "Admin", "Demo", "admin@resto.local", "555-1000", new DateOnly(2026, 1, 1));
        var ana = new Employee(centro.Id, salonCentro.Id, mesero.Id,
            "Ana", "Mesera", "ana@resto.local", "555-1001", new DateOnly(2026, 2, 1));
        db.Employees.AddRange(admin, ana);
        await db.SaveChangesAsync(cancellationToken); // admin.Id = 1

        await SeedMenuAsync(cancellationToken);

        logger.LogWarning(
            "Semilla de desarrollo de negocio creada: empresa 1, sucursales {C}/{N}, empleado admin id={A}.",
            centro.Id, norte.Id, admin.Id);
    }

    /// <summary>Carta demo: categorías, una tasa de IVA, ingredientes, un plato con receta e impuesto y una estación.</summary>
    private async Task SeedMenuAsync(CancellationToken cancellationToken)
    {
        if (await db.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var branchId = await db.Branches.OrderBy(b => b.Id).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);

        var entradas = new Category("Entradas", "Para empezar");
        var principales = new Category("Platos principales", "Fuertes");
        var bebidas = new Category("Bebidas", "Con y sin alcohol");
        db.Categories.AddRange(entradas, principales, bebidas);

        var iva = new TaxRate("IVA 21%", 21m);
        db.TaxRates.Add(iva);

        var harina = new Ingredient("Harina", "kg", 0.90m);
        var tomate = new Ingredient("Tomate", "kg", 1.40m);
        var mozzarella = new Ingredient("Mozzarella", "kg", 6.50m);
        db.Ingredients.AddRange(harina, tomate, mozzarella);
        await db.SaveChangesAsync(cancellationToken);

        var pizza = new MenuItem(principales.Id, "Pizza Margarita", "Salsa de tomate y mozzarella", 12.00m, true);
        pizza.SetRecipe([(harina.Id, 0.25m), (tomate.Id, 0.15m), (mozzarella.Id, 0.20m)]);
        pizza.SetTaxes([iva.Id]);
        db.MenuItems.Add(pizza);
        await db.SaveChangesAsync(cancellationToken);

        if (branchId > 0)
        {
            var cocinaCaliente = new KitchenStation(branchId, "Cocina caliente", "Horno y planchas");
            cocinaCaliente.SetMenuItems([pizza.Id]);
            db.KitchenStations.Add(cocinaCaliente);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Fase 5: mesas por sucursal + un cliente demo para probar reservas.</summary>
    private async Task SeedDiningRoomAsync(CancellationToken cancellationToken)
    {
        if (!await db.Customers.AnyAsync(cancellationToken))
        {
            db.Customers.Add(new Customer("Cliente", "Demo", "555-2000", "cliente@demo.local"));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Tables.AnyAsync(cancellationToken))
        {
            return;
        }

        var branches = await db.Branches.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(cancellationToken);
        foreach (var branchId in branches)
        {
            // 6 mesas de capacidades variadas por sucursal.
            int[] capacities = [2, 2, 4, 4, 6, 8];
            for (var i = 0; i < capacities.Length; i++)
            {
                db.Tables.Add(new Table(branchId, i + 1, capacities[i]));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Semilla de salón creada: {N} mesas ({B} sucursales).", branches.Count * 6, branches.Count);
    }

    /// <summary>Fase 6b: un descuento del catálogo y una tarjeta regalo con saldo para probar pagos.</summary>
    private async Task SeedSalesAsync(CancellationToken cancellationToken)
    {
        if (!await db.Discounts.AnyAsync(cancellationToken))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Discounts.Add(new Discount(
                "Happy Hour 10%", DiscountType.Percentage, 10m, today.AddYears(-1), today.AddYears(1)));
            db.Discounts.Add(new Discount(
                "Bono $5", DiscountType.FixedAmount, 5m, today.AddYears(-1), today.AddYears(1)));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.GiftCards.AnyAsync(cancellationToken))
        {
            var customerId = await db.Customers.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(cancellationToken);
            if (customerId > 0)
            {
                var card = new GiftCard
                {
                    CustomerId = customerId,
                    CardNumber = "GC-DEMO-0001",
                    ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2),
                };
                db.GiftCards.Add(card);
                // Balance tiene setter privado (se recarga en Fase 8); para la semilla se fija por EF.
                db.Entry(card).Property(nameof(GiftCard.Balance)).CurrentValue = 100m;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
