using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestoManager.Business.Domain.Organization;
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

        logger.LogWarning(
            "Semilla de desarrollo de negocio creada: empresa 1, sucursales {C}/{N}, empleado admin id={A}.",
            centro.Id, norte.Id, admin.Id);
    }
}
