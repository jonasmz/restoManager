using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestoManager.Business.Infrastructure.Persistence;

/// <summary>Solo para `dotnet ef` (migraciones): construye el contexto sin arrancar la Api.</summary>
public sealed class BusinessDbContextFactory : IDesignTimeDbContextFactory<BusinessDbContext>
{
    public BusinessDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__BusinessDb")
            ?? "Host=localhost;Port=5432;Database=resto_business;Username=resto;Password=resto";

        var options = new DbContextOptionsBuilder<BusinessDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new BusinessDbContext(options);
    }
}
