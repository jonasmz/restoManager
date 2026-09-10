using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Reports;

namespace RestoManager.Business.Api.Endpoints;

/// <summary>
/// Exportación de reportes a PDF (Fase 10). Rutas dedicadas <c>.../pdf</c> que
/// reflejan 1:1 los endpoints JSON de la Fase 9 (mismos filtros <c>?from=&amp;to=</c>
/// y <c>X-Branch-Id</c>). PDF server-side con QuestPDF; encabezado con restaurante,
/// sucursal, CUIT/NIF, período y emisión; pie con paginación. Política
/// <c>ReportsAccess</c> (ADMIN, BRANCH_MANAGER).
/// </summary>
public static class ReportPdfEndpoints
{
    public static void MapReportPdfEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reports").WithTags("Reportes (PDF)")
            .RequireAuthorization("ReportsAccess");

        Map(group, "/dashboard/pdf", "dashboard", "panel");
        Map(group, "/sales/summary/pdf", "sales-summary", "resumen-ventas");
        Map(group, "/sales/pdf", "sales", "ventas");
        Map(group, "/products/top/pdf", "products-top", "productos-top");
        Map(group, "/payments/by-method/pdf", "payments-by-method", "pagos-por-metodo");
        Map(group, "/discounts/applied/pdf", "discounts-applied", "descuentos-aplicados");
        Map(group, "/inventory/low-stock/pdf", "low-stock", "bajo-stock");
        Map(group, "/purchasing/cost/pdf", "purchasing-cost", "costo-compras");
        Map(group, "/tables/turnover/pdf", "tables-turnover", "rotacion-mesas");
    }

    private static void Map(RouteGroupBuilder group, string route, string kind, string fileStem)
    {
        group.MapGet(route, async (
            string? groupBy, int? limit, DateTime? from, DateTime? to,
            ReportDocumentBuilder builder, IReportRenderer renderer, IBranchContext branch,
            CancellationToken ct) =>
        {
            var scope = new ReportScope(
                branch.BranchId,
                from is { } f ? DateTime.SpecifyKind(f, DateTimeKind.Unspecified) : null,
                to is { } t ? DateTime.SpecifyKind(t, DateTimeKind.Unspecified) : null);

            var doc = await builder.BuildAsync(kind, scope, groupBy, limit ?? 20, ct);
            var pdf = renderer.ToPdf(doc);
            var name = $"{fileStem}-{DateTime.Now:yyyy-MM-dd}.pdf";
            return Results.File(pdf, "application/pdf", name);
        });
    }
}
