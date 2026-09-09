using RestoManager.Auth.Domain.Abstractions;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Domain.RefreshTokens;
using RestoManager.Auth.Domain.Tokens;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Application.Auth;

/// <summary>
/// Emisión y rotación de tokens. El refresh es de un solo uso: rotar consume el
/// actual y emite el siguiente en la misma sesión; presentar uno ya gastado revoca
/// toda la sesión (detección de reuso).
/// </summary>
public sealed class TokenService(
    ITokenIssuer issuer,
    IRefreshTokenRepository refreshTokens,
    IUserDirectory users,
    IUnitOfWork unitOfWork,
    IClock clock,
    AuthTokenOptions options)
{
    public async Task<TokenPair> StartSessionAsync(UserAccount user, CancellationToken cancellationToken = default)
    {
        var (pair, _) = BuildPair(user, Guid.NewGuid());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pair;
    }

    public async Task<TokenPair> RotateAsync(string presentedRefreshToken, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var hash = TokenHasher.Hash(presentedRefreshToken);
        var current = await refreshTokens.FindByHashAsync(hash, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        if (current.IsSpent)
        {
            await refreshTokens.RevokeSessionAsync(current.SessionId, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new RefreshTokenReuseException();
        }

        if (!current.IsActive(now))
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await users.FindByIdAsync(current.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var (pair, newHash) = BuildPair(user, current.SessionId);
        current.Consume(now, newHash);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pair;
    }

    public async Task RevokeAsync(string presentedRefreshToken, CancellationToken cancellationToken = default)
    {
        var current = await refreshTokens.FindByHashAsync(TokenHasher.Hash(presentedRefreshToken), cancellationToken);
        if (current is null)
        {
            return;
        }

        await refreshTokens.RevokeSessionAsync(current.SessionId, clock.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private (TokenPair pair, string refreshHash) BuildPair(UserAccount user, Guid sessionId)
    {
        var now = clock.UtcNow;
        var access = issuer.Issue(AccessTokenClaims.FromUser(user));

        var refreshValue = TokenHasher.NewSecret();
        var refreshHash = TokenHasher.Hash(refreshValue);
        refreshTokens.Add(RefreshToken.Issue(user.Id, sessionId, refreshHash, now, options.RefreshTokenLifetime));

        var expiresIn = (int)Math.Round((access.ExpiresAt - now).TotalSeconds);
        return (new TokenPair(access.Value, expiresIn, refreshValue), refreshHash);
    }
}
