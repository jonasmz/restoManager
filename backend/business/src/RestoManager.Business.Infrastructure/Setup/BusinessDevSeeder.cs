using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestoManager.Business.Domain.Customers;
using RestoManager.Business.Domain.DiningRoom;
using RestoManager.Business.Domain.Inventory;
using RestoManager.Business.Domain.Menu;
using RestoManager.Business.Domain.Delivery;
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
        await SeedBranchPublicSlugsAsync(cancellationToken);
        await SeedDiningRoomAsync(cancellationToken);
        await SeedSalesAsync(cancellationToken);
        await SeedDeliveryAsync(cancellationToken);
        await SeedLoyaltyAsync(cancellationToken);
        await SeedReorderPointsAsync(cancellationToken);
    }

    /// <summary>
    /// Fase 11: slug público de la carta por QR. Idempotente: solo asigna a las sucursales
    /// demo conocidas que aún no tengan slug.
    /// </summary>
    private async Task SeedBranchPublicSlugsAsync(CancellationToken cancellationToken)
    {
        var slugs = new Dictionary<string, string>
        {
            ["Sucursal Centro"] = "centro",
            ["Sucursal Norte"] = "norte",
        };

        var pending = await db.Branches
            .Where(b => b.PublicSlug == null && slugs.Keys.Contains(b.Name))
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        foreach (var branch in pending)
        {
            branch.SetPublicSlug(slugs[branch.Name]);
        }
        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Semilla Fase 11: slug público asignado a {N} sucursal(es).", pending.Count);
    }

    /// <summary>
    /// Fase 9: puntos de reposición demo para que el reporte de bajo stock tenga datos.
    /// Idempotente: solo toca insumos conocidos que aún estén en 0.
    /// </summary>
    private async Task SeedReorderPointsAsync(CancellationToken cancellationToken)
    {
        var defaults = new Dictionary<string, decimal>
        {
            ["Harina"] = 20m,
            ["Tomate"] = 15m,
            ["Mozzarella"] = 60m,
        };

        var pending = await db.Ingredients
            .Where(i => i.ReorderPoint == 0m && defaults.Keys.Contains(i.Name))
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var ingredient in pending)
        {
            ingredient.SetReorderPoint(defaults[ingredient.Name]);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Fase 7: un repartidor demo (empleado existente) para probar el canal DELIVERY.</summary>
    private async Task SeedDeliveryAsync(CancellationToken cancellationToken)
    {
        if (await db.DeliveryDrivers.AnyAsync(cancellationToken))
        {
            return;
        }

        var employeeId = await db.Employees.OrderBy(e => e.Id).Select(e => e.Id).Skip(1)
            .FirstOrDefaultAsync(cancellationToken);
        if (employeeId == 0)
        {
            employeeId = await db.Employees.OrderBy(e => e.Id).Select(e => e.Id).FirstOrDefaultAsync(cancellationToken);
        }
        if (employeeId > 0)
        {
            db.DeliveryDrivers.Add(new DeliveryDriver(employeeId, VehicleType.Motorcycle, "DEMO-001"));
            await db.SaveChangesAsync(cancellationToken);
        }
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

        // reorderPoint > 0 ⇒ entra en el reporte de bajo stock cuando el saldo cae (Fase 9).
        var harina = new Ingredient("Harina", "kg", 0.90m, reorderPoint: 20m);
        var tomate = new Ingredient("Tomate", "kg", 1.40m, reorderPoint: 15m);
        var mozzarella = new Ingredient("Mozzarella", "kg", 6.50m, reorderPoint: 60m);
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

    /// <summary>
    /// Fase 6: descuentos y tarjeta regalo demo (6b) + stock inicial por sucursal para
    /// que el descuento por receta de la 6c tenga saldo (6c).
    /// </summary>
    private async Task SeedSalesAsync(CancellationToken cancellationToken)
    {
        if (!await db.BranchInventories.AnyAsync(cancellationToken))
        {
            var branchIds = await db.Branches.Select(b => b.Id).ToListAsync(cancellationToken);
            var ingredientIds = await db.Ingredients.Select(i => i.Id).ToListAsync(cancellationToken);
            foreach (var branchId in branchIds)
            {
                foreach (var ingredientId in ingredientIds)
                {
                    // Semilla: saldo directo (el flujo de carga inicial con movimientos es Fase 3).
                    var balance = BranchInventory.Start(branchId, ingredientId);
                    balance.Apply(50m);
                    db.BranchInventories.Add(balance);
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }

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
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                db.GiftCards.Add(GiftCard.Issue(customerId, "GC-DEMO-0001", 100m, today.AddYears(2), today));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Fase 8: la fila de <c>discounts</c> de sistema que respalda el canje de puntos
    /// (la FK de <c>order_discounts</c> exige un descuento real) y unos puntos + reseña
    /// demo. Bloque idempotente.
    /// </summary>
    private async Task SeedLoyaltyAsync(CancellationToken cancellationToken)
    {
        const string systemDiscountName = "Canje de puntos de fidelidad";
        if (!await db.Discounts.AnyAsync(d => d.Name == systemDiscountName, cancellationToken))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Discounts.Add(new Discount(
                systemDiscountName, DiscountType.FixedAmount, 0.01m, today.AddYears(-5), today.AddYears(50)));
            await db.SaveChangesAsync(cancellationToken);
        }

        var demoCustomer = await db.Customers.OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
        if (demoCustomer is not null && demoCustomer.LoyaltyPoints == 0)
        {
            demoCustomer.AddLoyaltyPoints(500);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (demoCustomer is not null && !await db.Reviews.AnyAsync(cancellationToken))
        {
            var branchId = await db.Branches.OrderBy(b => b.Id).Select(b => b.Id).FirstOrDefaultAsync(cancellationToken);
            if (branchId > 0)
            {
                db.Reviews.Add(Review.Create(
                    demoCustomer.Id, branchId, 5, "Excelente servicio.", DateOnly.FromDateTime(DateTime.UtcNow)));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
