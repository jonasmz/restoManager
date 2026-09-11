namespace RestoManager.Business.Infrastructure.Setup;

/// <summary>Datos del empleado admin sembrado en desarrollo (sección <c>Seed:AdminEmployee</c>),
/// para que coincida con el admin de arranque de la Auth API (<c>Auth:BootstrapAdmin</c> —
/// ver deploy/.env.example). Solo aplica al crear la organización demo por primera vez.</summary>
public sealed class SeedAdminEmployeeOptions
{
    public const string SectionName = "Seed:AdminEmployee";

    public string FirstName { get; set; } = "Admin";
    public string LastName { get; set; } = "Demo";
    public string Email { get; set; } = "admin@resto.local";
    public string Phone { get; set; } = "555-1000";
}
