using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Infrastructure.Identity;

namespace RestoManager.Auth.Infrastructure.Setup;

/// <summary>Siembra los 5 roles fijos y, si está configurado, el admin de arranque.</summary>
public sealed class IdentityDataSeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
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

        if (await userManager.FindByEmailAsync(admin.Email) is not null)
        {
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
        }
        else
        {
            logger.LogError(
                "No se pudo crear el admin de arranque: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
        }
    }
}
