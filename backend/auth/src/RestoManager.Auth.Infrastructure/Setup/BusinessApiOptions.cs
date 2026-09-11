namespace RestoManager.Auth.Infrastructure.Setup;

/// <summary>Configuración para llamar a la Business API desde Auth (validar/vincular EmployeeId
/// al crear o sembrar un usuario — ver <see cref="RestoManager.Auth.Domain.Users.IEmployeeDirectoryClient"/>).
/// Es una llamada interna servicio-a-servicio (DNS de Docker), no pasa por nginx.</summary>
public sealed class BusinessApiOptions
{
    public const string SectionName = "Business";

    public string BaseUrl { get; set; } = "http://backend-business:8080";

    /// <summary>Debe coincidir con <c>Internal:ApiKey</c> de la Business API (deploy/.env).</summary>
    public string InternalApiKey { get; set; } = string.Empty;
}
