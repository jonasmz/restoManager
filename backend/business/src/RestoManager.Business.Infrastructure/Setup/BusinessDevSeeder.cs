using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Domain.Tax;
using RestoManager.Business.Infrastructure.Persistence;

namespace RestoManager.Business.Infrastructure.Setup;

/// <summary>
/// Semilla de desarrollo: 1 empresa, 2 sucursales, departamentos, roles/puestos y
/// empleados demo. El empleado id=1 (sucursal 1) corresponde al claim
/// <c>employee_id</c> del admin de arranque de la Auth API. Idempotente.
/// La reemplazará el seed real de la Fase 2 cuando se defina.
/// </summary>
public sealed class BusinessDevSeeder(BusinessDbContext db, ILogger<BusinessDevSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
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
}
