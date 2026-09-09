using Microsoft.EntityFrameworkCore;

namespace RestoManager.Business.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core de la base <c>resto_business</c>.
/// Vacío en Fase 0. La migración baseline que reproduce
/// <c>requirements/restaurant_schema.sql</c> se crea en la Fase 2.
/// </summary>
public sealed class BusinessDbContext(DbContextOptions<BusinessDbContext> options) : DbContext(options)
{
}
