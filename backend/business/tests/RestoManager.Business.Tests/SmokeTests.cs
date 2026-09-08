namespace RestoManager.Business.Tests;

/// <summary>
/// Prueba de humo de la Fase 0: la solución compila y los ensamblados de dominio
/// y aplicación son referenciables. Se sustituye por pruebas reales en la Fase 2.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Solution_builds_and_references_resolve()
    {
        var domain = typeof(RestoManager.Business.Domain.AssemblyMarker).Assembly;
        var application = typeof(RestoManager.Business.Application.AssemblyMarker).Assembly;

        Assert.NotNull(domain);
        Assert.NotNull(application);
    }
}
