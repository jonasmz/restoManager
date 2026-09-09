using RestoManager.Auth.Domain.Abstractions;
using RestoManager.Auth.Domain.RefreshTokens;
using RestoManager.Auth.Domain.Tokens;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Tests;

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

internal sealed class FakeTokenIssuer(IClock clock) : ITokenIssuer
{
    public int IssueCount { get; private set; }

    public AccessToken Issue(AccessTokenClaims claims)
    {
        IssueCount++;
        return new AccessToken($"access-{IssueCount}", clock.UtcNow.AddMinutes(15));
    }
}

internal sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];
    public int RevokeSessionCalls { get; private set; }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public void Add(RefreshToken token) => Tokens.Add(token);

    public Task RevokeSessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        RevokeSessionCalls++;
        foreach (var token in Tokens.Where(t => t.SessionId == sessionId && t.RevokedAt is null))
        {
            token.Revoke(now);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeUserDirectory(UserAccount user) : IUserDirectory
{
    public Task<UserAccount?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
        => Task.FromResult<UserAccount?>(email == user.Email && password == "correct" ? user : null);

    public Task<UserAccount?> FindByIdAsync(int userId, CancellationToken cancellationToken = default)
        => Task.FromResult<UserAccount?>(userId == user.Id ? user : null);

    public Task<int> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
