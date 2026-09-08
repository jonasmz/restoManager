using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Auth.Infrastructure.Persistence;

namespace RestoManager.Auth.Infrastructure;

/// <summary>
/// Registro de los adaptadores de salida de la Auth API (persistencia, etc.).
/// En Fase 0 solo se registra el <see cref="AuthDbContext"/> vacío.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        string? connectionString)
    {
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
