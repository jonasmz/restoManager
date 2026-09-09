namespace RestoManager.Auth.Domain.Tokens;

/// <summary>Access token JWT ya firmado, con su instante de expiración (UTC).</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
