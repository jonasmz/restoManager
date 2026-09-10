using System.Globalization;

namespace RestoManager.Business.Application.Reports;

/// <summary>
/// Arma un <see cref="ReportDocument"/> (KPIs + tablas) a partir de los resultados
/// de <see cref="IReportQueries"/>. Sin dependencia de QuestPDF ni de HTTP.
/// </summary>
public sealed class ReportDocumentBuilder(IReportQueries queries)
{
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

    public async Task<ReportDocument> BuildAsync(
        string kind, ReportScope scope, string? groupBy, int limit, CancellationToken ct = default)
    {
        var header = await queries.HeaderAsync(scope.BranchId, ct);
        var period = PeriodLabel(scope);
        var now = DateTime.Now;

        return kind switch
        {
            "dashboard" => await DashboardAsync(header, period, now, scope, ct),
            "sales-summary" => Doc("Resumen de ventas", header, period, now,
                await SummaryKpisAsync(scope, ct), []),
            "sales" => Doc($"Ventas por {GroupLabel(groupBy)}", header, period, now, [],
                [await SalesBucketSectionAsync(scope, groupBy ?? "channel", ct)]),
            "products-top" => Doc("Productos más vendidos", header, period, now, [],
                [await TopProductsSectionAsync(scope, limit, ct)]),
            "payments-by-method" => Doc("Pagos por método", header, period, now, [],
                [await PaymentsSectionAsync(scope, ct)]),
            "discounts-applied" => Doc("Descuentos aplicados", header, period, now, [],
                [await DiscountsSectionAsync(scope, ct)]),
            "low-stock" => Doc("Productos con bajo stock", header, period, now, [],
                [await LowStockSectionAsync(scope.BranchId, ct)]),
            "purchasing-cost" => Doc("Costo de compras", header, period, now,
                await PurchasingKpisAsync(scope, ct), []),
            "tables-turnover" => Doc("Rotación de mesas", header, period, now, [],
                [await TurnoverSectionAsync(scope, ct)]),
            _ => throw new ArgumentException($"Reporte '{kind}' desconocido.", nameof(kind)),
        };
    }

    // ─────────────────────────── Dashboard (informe completo) ───────────────────────────

    private async Task<ReportDocument> DashboardAsync(
        ReportHeader header, string period, DateTime now, ReportScope scope, CancellationToken ct)
    {
        var d = await queries.DashboardAsync(scope, ct);

        var kpis = new List<ReportKpi>
        {
            new("Ventas", Money(d.Kpis.Sales)),
            new("Compras", Money(d.Kpis.Purchases)),
            new("Pedidos", d.Kpis.Orders.ToString(Ar)),
            new("Ticket promedio", Money(d.Kpis.AverageTicket)),
            new("Bajo stock", d.Kpis.LowStockCount.ToString(Ar)),
        };

        var sections = new List<ReportSection>
        {
            new("Ventas por canal",
                ["Canal", "Pedidos", "Ventas"],
                d.SalesByChannel.Select(b => (IReadOnlyList<string>)
                    [b.Label, b.OrderCount.ToString(Ar), Money(b.TotalSales)]).ToList()),
            new("Ventas vs compras por día",
                ["Día", "Ventas", "Compras"],
                d.SalesVsPurchase.Select(p => (IReadOnlyList<string>)
                    [p.Date.ToString("dd/MM/yyyy", Ar), Money(p.Sales), Money(p.Purchases)]).ToList()),
            new("Más vendidos",
                ["Producto", "Unidades", "Monto"],
                d.TopSelling.Select(t => (IReadOnlyList<string>)
                    [t.Name, t.Units.ToString(Ar), Money(t.Amount)]).ToList()),
            new("Bajo stock",
                ["Insumo", "Stock", "Punto de reposición", "Unidad"],
                d.LowStock.Select(s => (IReadOnlyList<string>)
                    [s.Name, Num(s.StockQuantity), Num(s.ReorderPoint), s.Unit]).ToList()),
            new("Ventas recientes",
                ["Pedido", "Canal", "Estado", "Fecha", "Total"],
                d.RecentSales.Select(r => (IReadOnlyList<string>)
                    [$"#{r.OrderId}", r.Channel, r.Status, r.OrderTime.ToString("dd/MM/yyyy HH:mm", Ar), Money(r.TotalAmount)]).ToList()),
        };

        return new ReportDocument("Panel de gestión", header, period, now, kpis, sections);
    }

    // ─────────────────────────── Secciones individuales ───────────────────────────

    private async Task<IReadOnlyList<ReportKpi>> SummaryKpisAsync(ReportScope scope, CancellationToken ct)
    {
        var s = await queries.SalesSummaryAsync(scope, ct);
        return
        [
            new("Ventas", Money(s.TotalSales)),
            new("Pedidos", s.OrderCount.ToString(Ar)),
            new("Pagados", s.PaidCount.ToString(Ar)),
            new("Cerrados", s.ClosedCount.ToString(Ar)),
            new("Ticket promedio", Money(s.AverageTicket)),
            new("Descuentos", Money(s.DiscountTotal)),
        ];
    }

    private async Task<IReadOnlyList<ReportKpi>> PurchasingKpisAsync(ReportScope scope, CancellationToken ct)
    {
        var p = await queries.PurchasingCostAsync(scope, ct);
        return [new("Costo de compras", Money(p.TotalCost)), new("Órdenes recibidas", p.ReceivedOrders.ToString(Ar))];
    }

    private async Task<ReportSection> SalesBucketSectionAsync(ReportScope scope, string groupBy, CancellationToken ct)
    {
        var rows = await queries.SalesByAsync(scope, groupBy, ct);
        return new ReportSection(
            $"Ventas por {GroupLabel(groupBy)}",
            [GroupLabel(groupBy), "Pedidos", "Ventas"],
            rows.Select(b => (IReadOnlyList<string>)[b.Label, b.OrderCount.ToString(Ar), Money(b.TotalSales)]).ToList());
    }

    private async Task<ReportSection> TopProductsSectionAsync(ReportScope scope, int limit, CancellationToken ct)
    {
        var rows = await queries.TopProductsAsync(scope, limit <= 0 ? 20 : limit, ct);
        return new ReportSection("Productos más vendidos",
            ["Producto", "Unidades", "Monto"],
            rows.Select(t => (IReadOnlyList<string>)[t.Name, t.Units.ToString(Ar), Money(t.Amount)]).ToList());
    }

    private async Task<ReportSection> PaymentsSectionAsync(ReportScope scope, CancellationToken ct)
    {
        var rows = await queries.PaymentsByMethodAsync(scope, ct);
        return new ReportSection("Pagos por método",
            ["Método", "Cantidad", "Monto"],
            rows.Select(p => (IReadOnlyList<string>)[p.Method, p.Count.ToString(Ar), Money(p.Amount)]).ToList());
    }

    private async Task<ReportSection> DiscountsSectionAsync(ReportScope scope, CancellationToken ct)
    {
        var rows = await queries.DiscountsAppliedAsync(scope, ct);
        return new ReportSection("Descuentos aplicados",
            ["Descuento", "Veces", "Total"],
            rows.Select(d => (IReadOnlyList<string>)[d.Name, d.TimesApplied.ToString(Ar), Money(d.TotalAmount)]).ToList());
    }

    private async Task<ReportSection> LowStockSectionAsync(int branchId, CancellationToken ct)
    {
        var rows = await queries.LowStockAsync(branchId, ct);
        return new ReportSection("Productos con bajo stock",
            ["Insumo", "Stock", "Punto de reposición", "Unidad"],
            rows.Select(s => (IReadOnlyList<string>)[s.Name, Num(s.StockQuantity), Num(s.ReorderPoint), s.Unit]).ToList());
    }

    private async Task<ReportSection> TurnoverSectionAsync(ReportScope scope, CancellationToken ct)
    {
        var rows = await queries.TableTurnoverAsync(scope, ct);
        return new ReportSection("Rotación de mesas",
            ["Mesa", "Sesiones", "Duración media (min)", "Comensales medios"],
            rows.Select(t => (IReadOnlyList<string>)
                [t.TableNumber.ToString(Ar), t.Sessions.ToString(Ar),
                 t.AvgDurationMinutes.ToString("0.0", Ar), t.AvgGuests.ToString("0.0", Ar)]).ToList());
    }

    // ─────────────────────────── Helpers ───────────────────────────

    private static ReportDocument Doc(
        string title, ReportHeader header, string period, DateTime now,
        IReadOnlyList<ReportKpi> kpis, IReadOnlyList<ReportSection> sections)
        => new(title, header, period, now, kpis, sections);

    private static string Money(decimal v) => "$ " + v.ToString("N2", Ar);

    private static string Num(decimal v) => v.ToString("0.##", Ar);

    private static string GroupLabel(string? groupBy) => groupBy switch
    {
        "employee" => "empleado",
        "category" => "categoría",
        "day" => "día",
        _ => "canal",
    };

    private static string PeriodLabel(ReportScope scope)
    {
        if (scope.From is null && scope.To is null)
        {
            return "Sin filtro de fechas";
        }
        var from = scope.From?.ToString("dd/MM/yyyy", Ar) ?? "…";
        var to = scope.To?.ToString("dd/MM/yyyy", Ar) ?? "…";
        return $"{from} – {to}";
    }
}
