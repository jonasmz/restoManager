using RestoManager.Auth.Application.Auth;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Domain.RefreshTokens;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Tests;

public class TokenServiceTests
{
    private static readonly UserAccount User = new(1, "admin@resto.local", ["ADMIN"], 1, [1]);
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    private static (TokenService service, InMemoryRefreshTokenRepository repo, FakeClock clock, FakeUnitOfWork uow)
        Build()
    {
        var clock = new FakeClock(Now);
        var repo = new InMemoryRefreshTokenRepository();
        var uow = new FakeUnitOfWork();
        var service = new TokenService(
            new FakeTokenIssuer(clock), repo, new FakeUserDirectory(User), uow, clock,
            new AuthTokenOptions
            {
                AccessTokenLifetime = TimeSpan.FromMinutes(15),
                RefreshTokenLifetime = TimeSpan.FromDays(14),
            });
        return (service, repo, clock, uow);
    }

    [Fact]
    public async Task StartSession_issues_access_and_stored_refresh()
    {
        var (service, repo, _, uow) = Build();

        var pair = await service.StartSessionAsync(User);

        Assert.False(string.IsNullOrWhiteSpace(pair.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(pair.RefreshToken));
        Assert.Equal(15 * 60, pair.ExpiresInSeconds);
        Assert.Single(repo.Tokens);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Rotate_consumes_current_and_issues_next_in_same_session()
    {
        var (service, repo, _, _) = Build();
        var first = await service.StartSessionAsync(User);
        var sessionId = repo.Tokens.Single().SessionId;

        var second = await service.RotateAsync(first.RefreshToken);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.Equal(2, repo.Tokens.Count);
        Assert.NotNull(repo.Tokens[0].ConsumedAt);
        Assert.All(repo.Tokens, t => Assert.Equal(sessionId, t.SessionId));
        Assert.True(repo.Tokens[1].IsActive(Now));
    }

    [Fact]
    public async Task Rotate_with_already_consumed_token_revokes_session_and_throws_reuse()
    {
        var (service, repo, _, _) = Build();
        var first = await service.StartSessionAsync(User);
        await service.RotateAsync(first.RefreshToken); // consume #1

        var ex = await Assert.ThrowsAsync<RefreshTokenReuseException>(
            () => service.RotateAsync(first.RefreshToken)); // reuse #1

        Assert.Equal("auth.refresh_token_reuse", ex.Code);
        Assert.Equal(1, repo.RevokeSessionCalls);
        Assert.All(repo.Tokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task Rotate_with_unknown_token_throws_invalid()
        => await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => Build().service.RotateAsync("not-a-real-token"));

    [Fact]
    public async Task Rotate_with_expired_token_throws_invalid()
    {
        var (service, _, clock, _) = Build();
        var first = await service.StartSessionAsync(User);

        clock.UtcNow = Now.AddDays(15); // pasado el vencimiento del refresh (14 días)

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => service.RotateAsync(first.RefreshToken));
    }

    [Fact]
    public async Task Revoke_marks_whole_session_revoked()
    {
        var (service, repo, _, _) = Build();
        var pair = await service.StartSessionAsync(User);

        await service.RevokeAsync(pair.RefreshToken);

        Assert.Equal(1, repo.RevokeSessionCalls);
        Assert.All(repo.Tokens, t => Assert.NotNull(t.RevokedAt));
    }
}

public class AuthRolesTests
{
    [Fact]
    public void All_has_the_five_fixed_roles()
        => Assert.Equal(["ADMIN", "BRANCH_MANAGER", "WAITER", "KITCHEN", "INVENTORY"], AuthRoles.All);

    [Theory]
    [InlineData("ADMIN", true)]
    [InlineData("waiter", false)]
    [InlineData("SUPERADMIN", false)]
    public void IsValid_checks_membership(string role, bool expected)
        => Assert.Equal(expected, AuthRoles.IsValid(role));
}
