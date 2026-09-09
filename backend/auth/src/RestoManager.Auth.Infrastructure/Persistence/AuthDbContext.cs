using Microsoft.EntityFrameworkCore;

namespace RestoManager.Auth.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core de la base <c>resto_identity</c>.
/// Vacío en Fase 0; ASP.NET Core Identity y sus entidades se añaden en la Fase 1.
/// </summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
}
