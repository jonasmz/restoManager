namespace RestoManager.Business.Domain.Abstractions;

/// <summary>
/// Confirma los cambios acumulados. Las operaciones multi-registro (spec §1.1,
/// INV-05) se ejecutan con <see cref="ExecuteInTransactionAsync"/>.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Ejecuta <paramref name="action"/> y hace <c>SaveChanges</c> + commit en una sola transacción.</summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
