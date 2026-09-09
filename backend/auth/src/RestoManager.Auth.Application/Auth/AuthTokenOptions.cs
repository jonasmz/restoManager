namespace RestoManager.Auth.Application.Auth;

/// <summary>Duraciones de los tokens (decisión de Fase 1: 15 min / 14 días).</summary>
public sealed class AuthTokenOptions
{
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);
}
