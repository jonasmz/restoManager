using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestoManager.Business.Domain.Organization;
using RestoManager.Business.Infrastructure.Persistence;

namespace RestoManager.Business.Infrastructure.Setup;

/// <summary>
/// Semilla mínima de desarrollo: crea las filas padre de FK (1 restaurante, 1
/// sucursal, 1 departamento, 1 rol, 1 empleado) para poder ejercitar inventario y
/// compras antes de la Fase 2. La Fase 2 la reemplaza por su seed real.
/// </summary>
public sealed class BusinessDevSeeder(BusinessDbContext db, ILogger<BusinessDevSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Restaurants.Add(new Restaurant
        {
            Id = 1, Name = "Resto Manager Demo", Address = "Calle 1", Phone = "000", Email = "demo@resto.local", TaxNumber = "0",
        });
        db.Branches.Add(new Branch
        {
            Id = 1, RestaurantId = 1, Name = "Sucursal Centro", Address = "Calle 1", Phone = "000",
            Email = "centro@resto.local", OpeningTime = new TimeOnly(8, 0), ClosingTime = new TimeOnly(23, 0),
        });
        db.Departments.Add(new Department { Id = 1, BranchId = 1, Name = "Operaciones", Description = "-" });
        db.Roles.Add(new Role { Id = 1, Name = "ADMIN", Description = "Administrador", HourlyRate = 0m });
        db.Employees.Add(new Employee
        {
            Id = 1, BranchId = 1, DepartmentId = 1, RoleId = 1, FirstName = "Admin", LastName = "Demo",
            Email = "admin@resto.local", Phone = "000", HireDate = new DateOnly(2026, 1, 1),
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Semilla de desarrollo de negocio creada (restaurante/sucursal/empleado id=1).");
    }
}
