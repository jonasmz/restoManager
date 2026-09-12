using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Domain.Users;
using RestoManager.Auth.Infrastructure.Identity;

namespace RestoManager.Auth.Infrastructure.Setup;

/// <summary>Siembra los 5 roles fijos y, si está configurado, el admin de arranque — y lo
/// vincula al empleado admin sembrado por la Business API (ver BusinessDevSeeder). El intento
/// de vínculo se repite en cada arranque (no solo al crear el usuario), por si Business no
/// estaba listo en un boot anterior: es idempotente del lado de Business.</summary>
public sealed class IdentityDataSeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IEmployeeDirectoryClient employeeDirectory,
    AuthOptions options,
    ILogger<IdentityDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in AuthRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }

        var admin = options.BootstrapAdmin;
        if (admin is null || admin.Email.Length == 0 || admin.Password.Length == 0)
        {
            return;
        }

        var existing = await userManager.FindByEmailAsync(admin.Email);
        if (existing is not null)
        {
            await LinkToBusinessAsync(existing.Id, admin.EmployeeId, cancellationToken);
            return;
        }

        var user = new AppUser
        {
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
            EmployeeId = admin.EmployeeId,
            BranchIdsCsv = admin.BranchId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        var created = await userManager.CreateAsync(user, admin.Password);
        if (created.Succeeded)
        {
            await userManager.AddToRoleAsync(user, AuthRoles.Admin);
            logger.LogWarning("Usuario admin de arranque creado: {Email}", admin.Email);
            await LinkToBusinessAsync(user.Id, admin.EmployeeId, cancellationToken);
        }
        else
        {
            logger.LogError(
                "No se pudo crear el admin de arranque: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
        }
    }

    private async Task LinkToBusinessAsync(int userId, int employeeId, CancellationToken cancellationToken)
    {
        var linked = await employeeDirectory.LinkUserAsync(employeeId, userId, cancellationToken);
        if (!linked)
        {
            logger.LogWarning(
                "No se pudo vincular el admin de arranque (userId={UserId}) con el empleado {EmployeeId} en Business; " +
                "se reintentará en el próximo arranque.", userId, employeeId);
        }
    }
}
