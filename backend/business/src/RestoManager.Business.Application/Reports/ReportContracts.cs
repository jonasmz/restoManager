namespace RestoManager.Business.Application.Reports;

/// <summary>
/// Consultas analíticas de solo lectura (Fase 9). Todas se calculan por consulta
/// sobre datos transaccionales (MET-02): no hay tablas de agregados.
///
/// Criterio de <b>venta efectiva</b> (MET-01): <c>orders.status IN ('PAID','CLOSED')</c>;
/// se excluyen <c>OPEN</c> y <c>CANCELLED</c>. Medios de pago: solo
/// <c>payments.status = 'CONFIRMED'</c>.
/// </summary>
public interface IReportQueries
{
    Task<SalesSummary> SalesSummaryAsync(ReportScope scope, CancellationToken ct = default);

    /// <param name="groupBy">channel | employee | category | day</param>
    Task<IReadOnlyList<SalesBucket>> SalesByAsync(ReportScope scope, string groupBy, CancellationToken ct = default);

    Task<IReadOnlyList<TopProduct>> TopProductsAsync(ReportScope scope, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<PaymentMethodTotal>> PaymentsByMethodAsync(ReportScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<DiscountApplied>> DiscountsAppliedAsync(ReportScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<LowStockLine>> LowStockAsync(int branchId, CancellationToken ct = default);

    Task<PurchasingCost> PurchasingCostAsync(ReportScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<TableTurnover>> TableTurnoverAsync(ReportScope scope, CancellationToken ct = default);

    Task<DashboardPayload> DashboardAsync(ReportScope scope, CancellationToken ct = default);
}

/// <summary>Filtros comunes de un reporte. <paramref name="From"/>/<paramref name="To"/> se aplican sobre <c>order_time</c> (medias-abiertas [from, to)).</summary>
public sealed record ReportScope(int BranchId, DateTime? From, DateTime? To);

public sealed record SalesSummary(
    decimal TotalSales, int OrderCount, decimal AverageTicket,
    int PaidCount, int ClosedCount, decimal DiscountTotal);

public sealed record SalesBucket(string Key, string Label, int OrderCount, decimal TotalSales);

public sealed record TopProduct(int MenuItemId, string Name, int Units, decimal Amount);

public sealed record PaymentMethodTotal(string Method, int Count, decimal Amount);

public sealed record DiscountApplied(int DiscountId, string Name, int TimesApplied, decimal TotalAmount);

public sealed record LowStockLine(
    int IngredientId, string Name, string Unit, decimal StockQuantity, decimal ReorderPoint);

public sealed record PurchasingCost(decimal TotalCost, int ReceivedOrders);

public sealed record TableTurnover(
    int TableId, int TableNumber, int Sessions, double AvgDurationMinutes, double AvgGuests);

public sealed record DashboardKpis(
    decimal Sales, int Orders, decimal AverageTicket, decimal Purchases, int LowStockCount);

public sealed record DayPoint(DateOnly Date, decimal Sales, decimal Purchases);

public sealed record RecentSale(
    int OrderId, string Channel, string Status, decimal TotalAmount, DateTime OrderTime);

public sealed record DashboardPayload(
    DashboardKpis Kpis,
    IReadOnlyList<DayPoint> SalesVsPurchase,
    IReadOnlyList<SalesBucket> SalesByChannel,
    IReadOnlyList<TopProduct> TopSelling,
    IReadOnlyList<LowStockLine> LowStock,
    IReadOnlyList<RecentSale> RecentSales);
