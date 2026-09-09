namespace RestoManager.Auth.Domain.Abstractions;

/// <summary>Confirma en una sola transacción los cambios acumulados en el contexto.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
