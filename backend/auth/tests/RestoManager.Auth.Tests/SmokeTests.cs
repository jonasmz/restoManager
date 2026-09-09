namespace RestoManager.Auth.Tests;

/// <summary>
/// Prueba de humo de la Fase 0: la solución compila y los ensamblados de dominio
/// y aplicación son referenciables. Se sustituye por pruebas reales en la Fase 1.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Solution_builds_and_references_resolve()
    {
        var domain = typeof(RestoManager.Auth.Domain.AssemblyMarker).Assembly;
        var application = typeof(RestoManager.Auth.Application.AssemblyMarker).Assembly;

        Assert.NotNull(domain);
        Assert.NotNull(application);
    }
}
