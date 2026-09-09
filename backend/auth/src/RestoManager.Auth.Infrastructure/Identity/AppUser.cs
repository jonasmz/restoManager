using Microsoft.AspNetCore.Identity;

namespace RestoManager.Auth.Infrastructure.Identity;

/// <summary>
/// Usuario de ASP.NET Core Identity. En Fase 1 lleva denormalizados el empleado y
/// sus sucursales; la Fase 2 los reconcilia con la tabla <c>employees</c> del negocio.
/// </summary>
public sealed class AppUser : IdentityUser<int>
{
    public int EmployeeId { get; set; }

    /// <summary>Ids de sucursal separados por coma (p. ej. "1" o "1,3").</summary>
    public string BranchIdsCsv { get; set; } = string.Empty;
}
