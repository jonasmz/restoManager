using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Domain.Abstractions;

namespace RestoManager.Business.Infrastructure.Persistence;

public sealed class UnitOfWork(BusinessDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
