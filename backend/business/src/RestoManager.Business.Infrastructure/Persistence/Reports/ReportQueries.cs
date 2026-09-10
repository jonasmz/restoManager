using Microsoft.EntityFrameworkCore;
using RestoManager.Business.Application.Reports;
using RestoManager.Business.Domain.Purchasing;
using RestoManager.Business.Domain.Sales;

namespace RestoManager.Business.Infrastructure.Persistence.Reports;

/// <summary>
/// Implementación de <see cref="IReportQueries"/>: lecturas directas sobre
/// <see cref="BusinessDbContext"/>. Sin entidades ni tablas de agregados (MET-02).
/// Venta efectiva = <c>orders.status IN ('PAID','CLOSED')</c> (MET-01).
/// </summary>
public sealed class ReportQueries(BusinessDbContext db) : IReportQueries
{
    private IQueryable<Order> EffectiveOrders(ReportScope scope) => db.Orders.Where(o =>
        o.BranchId == scope.BranchId
        && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Closed)
        && (scope.From == null || o.OrderTime >= scope.From)
        && (scope.To == null || o.OrderTime < scope.To));

    public async Task<SalesSummary> SalesSummaryAsync(ReportScope scope, CancellationToken ct = default)
    {
        var q = EffectiveOrders(scope);

        var total = await q.SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;
        var count = await q.CountAsync(ct);
        var paid = await q.CountAsync(o => o.Status == OrderStatus.Paid, ct);
        var closed = await q.CountAsync(o => o.Status == OrderStatus.Closed, ct);
        var discounts = await (from od in db.OrderDiscounts
                               join o in q on od.OrderId equals o.Id
                               select od.AppliedAmount).SumAsync(x => (decimal?)x, ct) ?? 0m;

        var avg = count == 0 ? 0m : Math.Round(total / count, 2, MidpointRounding.AwayFromZero);
        return new SalesSummary(total, count, avg, paid, closed, discounts);
    }

    public async Task<IReadOnlyList<SalesBucket>> SalesByAsync(
        ReportScope scope, string groupBy, CancellationToken ct = default)
    {
        var q = EffectiveOrders(scope);

        switch (groupBy?.ToLowerInvariant())
        {
            case "channel":
            {
                var rows = await q.GroupBy(o => o.Channel)
                    .Select(g => new { Key = g.Key, Orders = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                    .ToListAsync(ct);
                return rows
                    .Select(r => new SalesBucket(
                        r.Key.ToDbValue(), ChannelLabel(r.Key), r.Orders, r.Total))
                    .OrderByDescending(b => b.TotalSales)
                    .ToList();
            }

            case "employee":
            {
                var rows = await q.GroupBy(o => o.EmployeeId)
                    .Select(g => new { EmployeeId = g.Key, Orders = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                    .ToListAsync(ct);
                var ids = rows.Select(r => r.EmployeeId).ToList();
                var names = await db.Employees.Where(e => ids.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id, e => $"{e.LastName}, {e.FirstName}", ct);
                return rows
                    .Select(r => new SalesBucket(
                        r.EmployeeId.ToString(),
                        names.TryGetValue(r.EmployeeId, out var n) ? n : $"#{r.EmployeeId}",
                        r.Orders, r.Total))
                    .OrderByDescending(b => b.TotalSales)
                    .ToList();
            }

            case "category":
            {
                var totals = await (from oi in db.OrderItems
                                    join o in q on oi.OrderId equals o.Id
                                    join mi in db.MenuItems on oi.MenuItemId equals mi.Id
                                    group oi by mi.CategoryId into g
                                    select new { CategoryId = g.Key, Total = g.Sum(x => x.Quantity * x.UnitPrice) })
                    .ToListAsync(ct);

                var orderCounts = await (from oi in db.OrderItems
                                         join o in q on oi.OrderId equals o.Id
                                         join mi in db.MenuItems on oi.MenuItemId equals mi.Id
                                         select new { mi.CategoryId, o.Id })
                    .Distinct()
                    .GroupBy(x => x.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Orders = g.Count() })
                    .ToListAsync(ct);

                var catIds = totals.Select(t => t.CategoryId).ToList();
                var catNames = await db.Categories.Where(c => catIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
                var orderCountByCat = orderCounts.ToDictionary(x => x.CategoryId, x => x.Orders);

                return totals
                    .Select(t => new SalesBucket(
                        t.CategoryId.ToString(),
                        catNames.TryGetValue(t.CategoryId, out var n) ? n : $"#{t.CategoryId}",
                        orderCountByCat.TryGetValue(t.CategoryId, out var oc) ? oc : 0,
                        t.Total))
                    .OrderByDescending(b => b.TotalSales)
                    .ToList();
            }

            case "day":
            {
                var rows = await q.GroupBy(o => o.OrderTime.Date)
                    .Select(g => new { Day = g.Key, Orders = g.Count(), Total = g.Sum(o => o.TotalAmount) })
                    .ToListAsync(ct);
                return rows
                    .OrderBy(r => r.Day)
                    .Select(r => new SalesBucket(
                        DateOnly.FromDateTime(r.Day).ToString("yyyy-MM-dd"),
                        DateOnly.FromDateTime(r.Day).ToString("dd/MM"),
                        r.Orders, r.Total))
                    .ToList();
            }

            default:
                throw new ArgumentException(
                    $"groupBy '{groupBy}' no soportado. Use channel, employee, category o day.", nameof(groupBy));
        }
    }

    public async Task<IReadOnlyList<TopProduct>> TopProductsAsync(
        ReportScope scope, int limit, CancellationToken ct = default)
    {
        var q = EffectiveOrders(scope);
        var rows = await (from oi in db.OrderItems
                          join o in q on oi.OrderId equals o.Id
                          join mi in db.MenuItems on oi.MenuItemId equals mi.Id
                          group oi by new { mi.Id, mi.Name } into g
                          select new
                          {
                              g.Key.Id,
                              g.Key.Name,
                              Units = g.Sum(x => x.Quantity),
                              Amount = g.Sum(x => x.Quantity * x.UnitPrice),
                          })
            .OrderByDescending(x => x.Units)
            .Take(limit <= 0 ? 5 : limit)
            .ToListAsync(ct);

        return rows.Select(r => new TopProduct(r.Id, r.Name, r.Units, r.Amount)).ToList();
    }

    public async Task<IReadOnlyList<PaymentMethodTotal>> PaymentsByMethodAsync(
        ReportScope scope, CancellationToken ct = default)
    {
        var rows = await (from p in db.Payments
                          join o in db.Orders on p.OrderId equals o.Id
                          where o.BranchId == scope.BranchId
                              && p.Status == PaymentStatus.Confirmed
                              && (scope.From == null || p.PaymentTime >= scope.From)
                              && (scope.To == null || p.PaymentTime < scope.To)
                          group p by p.PaymentMethod into g
                          select new { Method = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        return rows
            .Select(r => new PaymentMethodTotal(r.Method.ToDbValue(), r.Count, r.Amount))
            .OrderByDescending(r => r.Amount)
            .ToList();
    }

    public async Task<IReadOnlyList<DiscountApplied>> DiscountsAppliedAsync(
        ReportScope scope, CancellationToken ct = default)
    {
        var q = EffectiveOrders(scope);
        var rows = await (from od in db.OrderDiscounts
                          join o in q on od.OrderId equals o.Id
                          join d in db.Discounts on od.DiscountId equals d.Id
                          group od by new { d.Id, d.Name } into g
                          select new
                          {
                              g.Key.Id,
                              g.Key.Name,
                              Times = g.Count(),
                              Total = g.Sum(x => x.AppliedAmount),
                          })
            .ToListAsync(ct);

        return rows
            .Select(r => new DiscountApplied(r.Id, r.Name, r.Times, r.Total))
            .OrderByDescending(r => r.TotalAmount)
            .ToList();
    }

    public async Task<IReadOnlyList<LowStockLine>> LowStockAsync(int branchId, CancellationToken ct = default)
        => await (from bi in db.BranchInventories
                  join i in db.Ingredients on bi.IngredientId equals i.Id
                  where bi.BranchId == branchId && i.ReorderPoint > 0m && bi.StockQuantity <= i.ReorderPoint
                  orderby bi.StockQuantity / i.ReorderPoint
                  select new LowStockLine(i.Id, i.Name, i.Unit, bi.StockQuantity, i.ReorderPoint))
            .ToListAsync(ct);

    public async Task<PurchasingCost> PurchasingCostAsync(ReportScope scope, CancellationToken ct = default)
    {
        DateOnly? fromDate = scope.From is { } f ? DateOnly.FromDateTime(f) : null;
        DateOnly? toDate = scope.To is { } t ? DateOnly.FromDateTime(t) : null;

        var pos = db.PurchaseOrders.Where(po =>
            po.BranchId == scope.BranchId
            && po.Status == PurchaseOrderStatus.Received
            && (fromDate == null || po.OrderDate >= fromDate)
            && (toDate == null || po.OrderDate < toDate));

        var total = await (from poi in db.PurchaseOrderItems
                           join po in pos on poi.PurchaseOrderId equals po.Id
                           select poi.Quantity * poi.UnitPrice).SumAsync(x => (decimal?)x, ct) ?? 0m;
        var count = await pos.CountAsync(ct);
        return new PurchasingCost(total, count);
    }

    public async Task<IReadOnlyList<TableTurnover>> TableTurnoverAsync(
        ReportScope scope, CancellationToken ct = default)
    {
        var sessions = await (from ts in db.TableSessions
                              join t in db.Tables on ts.TableId equals t.Id
                              where t.BranchId == scope.BranchId
                                  && ts.ClosedAt != null
                                  && (scope.From == null || ts.OpenedAt >= scope.From)
                                  && (scope.To == null || ts.OpenedAt < scope.To)
                              select new
                              {
                                  ts.TableId,
                                  t.Number,
                                  ts.OpenedAt,
                                  ClosedAt = ts.ClosedAt!.Value,
                                  ts.GuestCount,
                              })
            .ToListAsync(ct);

        return sessions
            .GroupBy(s => new { s.TableId, s.Number })
            .Select(g => new TableTurnover(
                g.Key.TableId,
                g.Key.Number,
                g.Count(),
                Math.Round(g.Average(s => (s.ClosedAt - s.OpenedAt).TotalMinutes), 1),
                Math.Round(g.Average(s => (double)s.GuestCount), 1)))
            .OrderByDescending(t => t.Sessions)
            .ToList();
    }

    public async Task<DashboardPayload> DashboardAsync(ReportScope scope, CancellationToken ct = default)
    {
        // El dashboard siempre trabaja sobre una ventana acotada (por defecto 30 días).
        var windowTo = scope.To ?? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var windowFrom = scope.From ?? windowTo.AddDays(-30);
        var window = scope with { From = windowFrom, To = windowTo };
        var fromDate = DateOnly.FromDateTime(windowFrom);
        var toDate = DateOnly.FromDateTime(windowTo);

        var summary = await SalesSummaryAsync(window, ct);
        var purchases = await PurchasingCostAsync(window, ct);
        var byChannel = await SalesByAsync(window, "channel", ct);
        var byDay = await SalesByAsync(window, "day", ct);
        var top = await TopProductsAsync(window, 5, ct);
        var lowStock = await LowStockAsync(scope.BranchId, ct);

        var purchasesByDay = await (from poi in db.PurchaseOrderItems
                                    join po in db.PurchaseOrders on poi.PurchaseOrderId equals po.Id
                                    where po.BranchId == scope.BranchId
                                        && po.Status == PurchaseOrderStatus.Received
                                        && po.OrderDate >= fromDate
                                        && po.OrderDate < toDate
                                    group poi by po.OrderDate into g
                                    select new { Day = g.Key, Total = g.Sum(x => x.Quantity * x.UnitPrice) })
            .ToListAsync(ct);
        var purchaseByDay = purchasesByDay.ToDictionary(x => x.Day, x => x.Total);

        var salesVsPurchase = byDay
            .Select(b =>
            {
                var d = DateOnly.ParseExact(b.Key, "yyyy-MM-dd");
                return new DayPoint(d, b.TotalSales, purchaseByDay.TryGetValue(d, out var p) ? p : 0m);
            })
            .ToList();
        // Días con compra pero sin venta.
        foreach (var (day, amount) in purchaseByDay)
        {
            if (salesVsPurchase.All(x => x.Date != day))
            {
                salesVsPurchase.Add(new DayPoint(day, 0m, amount));
            }
        }
        salesVsPurchase = salesVsPurchase.OrderBy(x => x.Date).ToList();

        var recent = await EffectiveOrders(scope with { From = null, To = null })
            .OrderByDescending(o => o.Id)
            .Take(8)
            .Select(o => new RecentSale(
                o.Id, o.Channel.ToDbValue(), o.Status.ToDbValue(), o.TotalAmount, o.OrderTime))
            .ToListAsync(ct);

        var kpis = new DashboardKpis(
            summary.TotalSales, summary.OrderCount, summary.AverageTicket,
            purchases.TotalCost, lowStock.Count);

        return new DashboardPayload(
            kpis, salesVsPurchase, byChannel, top,
            lowStock.Take(8).ToList(), recent);
    }

    public async Task<ReportHeader> HeaderAsync(int branchId, CancellationToken ct = default)
    {
        var row = await (from b in db.Branches
                         join r in db.Restaurants on b.RestaurantId equals r.Id
                         where b.Id == branchId
                         select new { Branch = b.Name, Restaurant = r.Name, r.TaxNumber })
            .FirstOrDefaultAsync(ct);

        return row is null
            ? new ReportHeader("—", $"Sucursal #{branchId}", "—")
            : new ReportHeader(row.Restaurant, row.Branch, row.TaxNumber);
    }

    private static string ChannelLabel(OrderChannel c) => c switch
    {
        OrderChannel.Mesa => "Mesa",
        OrderChannel.Barra => "Barra",
        OrderChannel.Takeaway => "Para llevar",
        OrderChannel.Delivery => "Delivery",
        _ => c.ToString(),
    };
}
