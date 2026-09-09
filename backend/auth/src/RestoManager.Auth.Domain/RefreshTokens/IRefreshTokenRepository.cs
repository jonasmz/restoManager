namespace RestoManager.Auth.Domain.RefreshTokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    void Add(RefreshToken token);

    /// <summary>Revoca todos los tokens activos de la sesión (detección de reuso / logout).</summary>
    Task RevokeSessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
