using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Application.Reports;

namespace RestoManager.Business.Api.Endpoints;

/// <summary>
/// Reportes y dashboard (Fase 9). Solo lectura, sucursal activa (X-Branch-Id).
/// Filtros comunes por query string: <c>?from=&amp;to=</c> (ISO 8601, ventana
/// media-abierta <c>[from, to)</c> sobre <c>order_time</c>). Política
/// <c>ReportsAccess</c> (ADMIN, BRANCH_MANAGER).
///
/// Venta efectiva (MET-01): <c>orders.status IN ('PAID','CLOSED')</c>.
/// Medios de pago: <c>payments.status = 'CONFIRMED'</c>.
/// </summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reports").WithTags("Reportes")
            .RequireAuthorization("ReportsAccess");

        group.MapGet("/dashboard", async (
            DateTime? from, DateTime? to, IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.DashboardAsync(Scope(branch, from, to), ct)));

        group.MapGet("/sales/summary", async (
            DateTime? from, DateTime? to, IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.SalesSummaryAsync(Scope(branch, from, to), ct)));

        group.MapGet("/sales", async (
            string groupBy, DateTime? from, DateTime? to,
            IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.SalesByAsync(Scope(branch, from, to), groupBy, ct)));

        group.MapGet("/products/top", async (
            int? limit, DateTime? from, DateTime? to,
            IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.TopProductsAsync(Scope(branch, from, to), limit ?? 5, ct)));

        group.MapGet("/payments/by-method", async (
            DateTime? from, DateTime? to, IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.PaymentsByMethodAsync(Scope(branch, from, to), ct)));

        group.MapGet("/discounts/applied", async (
            DateTime? from, DateTime? to, IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.DiscountsAppliedAsync(Scope(branch, from, to), ct)));

        group.MapGet("/inventory/low-stock", async (
            IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.LowStockAsync(branch.BranchId, ct)));

        group.MapGet("/purchasing/cost", async (
            DateTime? from, DateTime? to, IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.PurchasingCostAsync(Scope(branch, from, to), ct)));

        group.MapGet("/tables/turnover", async (
            DateTime? from, DateTime? to, IReportQueries q, IBranchContext branch, CancellationToken ct) =>
            Results.Ok(await q.TableTurnoverAsync(Scope(branch, from, to), ct)));
    }

    private static ReportScope Scope(IBranchContext branch, DateTime? from, DateTime? to) => new(
        branch.BranchId,
        from is { } f ? DateTime.SpecifyKind(f, DateTimeKind.Unspecified) : null,
        to is { } t ? DateTime.SpecifyKind(t, DateTimeKind.Unspecified) : null);
}
