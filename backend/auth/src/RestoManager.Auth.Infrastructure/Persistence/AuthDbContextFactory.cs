using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestoManager.Auth.Infrastructure.Persistence;

/// <summary>
/// Solo para `dotnet ef` (migraciones): construye el contexto sin arrancar la Api.
/// Toma la cadena de <c>ConnectionStrings__IdentityDb</c> o un valor local por defecto.
/// </summary>
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__IdentityDb")
            ?? "Host=localhost;Port=5432;Database=resto_identity;Username=resto;Password=resto";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AuthDbContext(options);
    }
}
