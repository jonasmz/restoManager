namespace RestoManager.Business.Domain.Abstractions;

/// <summary>Reloj inyectable (testabilidad). Siempre en UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
