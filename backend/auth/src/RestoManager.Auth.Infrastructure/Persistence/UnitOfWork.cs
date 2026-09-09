using RestoManager.Auth.Domain.Abstractions;

namespace RestoManager.Auth.Infrastructure.Persistence;

public sealed class UnitOfWork(AuthDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
