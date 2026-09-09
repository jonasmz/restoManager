namespace RestoManager.Auth.Infrastructure.Setup;

/// <summary>Configuración de la Auth API (sección <c>Auth</c> de appsettings / env vars).</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "http://localhost:5001";
    public string Audience { get; set; } = "resto";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>Carpeta de claves RSA (relativa al content root). `current.pem` obligatoria, `next.pem` opcional.</summary>
    public string SigningKeysDirectory { get; set; } = "keys";

    public BootstrapAdminOptions? BootstrapAdmin { get; set; }
}

/// <summary>Admin de arranque para desarrollo (si se define, se crea al iniciar).</summary>
public sealed class BootstrapAdminOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int BranchId { get; set; }
}
