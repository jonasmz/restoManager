namespace RestoManager.Auth.Domain.RefreshTokens;

/// <summary>
/// Refresh token de un solo uso. Los tokens de una misma sesión de login comparten
/// <see cref="SessionId"/>; rotar consume el actual y emite el siguiente. Presentar
/// uno ya consumido o revocado es indicio de robo y revoca la sesión completa.
/// </summary>
public sealed class RefreshToken
{
    public long Id { get; private set; }
    public int UserId { get; private set; }
    public Guid SessionId { get; private set; }

    /// <summary>SHA-256 (Base64) del valor del token. El valor en claro solo lo ve el cliente.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByHash { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(
        int userId, Guid sessionId, string tokenHash, DateTimeOffset now, TimeSpan lifetime) => new()
    {
        UserId = userId,
        SessionId = sessionId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now.Add(lifetime),
    };

    public bool IsActive(DateTimeOffset now) =>
        ConsumedAt is null && RevokedAt is null && ExpiresAt > now;

    public bool IsSpent => ConsumedAt is not null || RevokedAt is not null;

    public void Consume(DateTimeOffset now, string replacedByHash)
    {
        ConsumedAt = now;
        ReplacedByHash = replacedByHash;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
