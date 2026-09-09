using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestoManager.Auth.Domain.RefreshTokens;
using RestoManager.Auth.Infrastructure.Identity;
using RestoManager.Auth.Infrastructure.Persistence.Configurations;

namespace RestoManager.Auth.Infrastructure.Persistence;

/// <summary>Contexto EF Core de la base <c>resto_identity</c> (Identity + refresh tokens).</summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<AppUser, AppRole, int>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
