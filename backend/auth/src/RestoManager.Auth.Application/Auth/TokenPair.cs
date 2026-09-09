namespace RestoManager.Auth.Application.Auth;

/// <summary>Par de tokens devuelto por login y refresh.</summary>
public sealed record TokenPair(string AccessToken, int ExpiresInSeconds, string RefreshToken);
