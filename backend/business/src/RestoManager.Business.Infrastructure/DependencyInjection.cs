using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Business.Infrastructure.Persistence;

namespace RestoManager.Business.Infrastructure;

/// <summary>
/// Registro de los adaptadores de salida de la Business API (persistencia, etc.).
/// En Fase 0 solo se registra el <see cref="BusinessDbContext"/> vacío.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessInfrastructure(
        this IServiceCollection services,
        string? connectionString)
    {
        services.AddDbContext<BusinessDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
