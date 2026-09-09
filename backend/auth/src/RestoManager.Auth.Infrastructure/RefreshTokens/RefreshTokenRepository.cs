using Microsoft.EntityFrameworkCore;
using RestoManager.Auth.Domain.RefreshTokens;
using RestoManager.Auth.Infrastructure.Persistence;

namespace RestoManager.Auth.Infrastructure.RefreshTokens;

public sealed class RefreshTokenRepository(AuthDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    public Task RevokeSessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default)
        => context.RefreshTokens
            .Where(x => x.SessionId == sessionId && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, _ => now), cancellationToken);
}
