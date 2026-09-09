namespace RestoManager.Auth.Domain.Abstractions;

/// <summary>Reloj inyectable (testabilidad). Siempre en UTC.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
