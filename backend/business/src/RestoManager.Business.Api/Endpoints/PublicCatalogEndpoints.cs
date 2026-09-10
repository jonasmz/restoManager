using RestoManager.Business.Application.PublicCatalog;

namespace RestoManager.Business.Api.Endpoints;

/// <summary>
/// Carta pública accesible por QR (Fase 11). <b>Sin autenticación</b>: se mapea fuera del
/// bloque de auth de <c>Program.cs</c> y no aplica ninguna política. La sucursal se
/// resuelve por su slug público, nunca por <c>X-Branch-Id</c> ni por claims del token.
/// </summary>
public static class PublicCatalogEndpoints
{
    public static void MapPublicCatalogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/public").WithTags("Carta pública");

        group.MapGet("/catalog/{slug}", async (
            string slug, GetPublicCatalogHandler h, CancellationToken ct) =>
            Results.Ok(await h.HandleAsync(slug, ct)));
    }
}
