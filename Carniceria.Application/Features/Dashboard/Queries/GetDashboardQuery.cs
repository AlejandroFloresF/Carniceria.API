using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Dashboard.Queries;

public record GetDashboardQuery(
    DateTime From,
    DateTime To,
    Guid? SessionId
) : IRequest<Result<DashboardDto>>;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly IDashboardRepository _dashboard;
    private readonly ICustomerDebtRepository _debts;

    public GetDashboardHandler(
        IDashboardRepository dashboard,
        ICustomerDebtRepository debts)
    {
        _dashboard = dashboard;
        _debts = debts;
    }

    public async Task<Result<DashboardDto>> Handle(
        GetDashboardQuery q, CancellationToken ct)
    {
        // ── 1. Órdenes del período ────────────────────────────────────────────
        var orders = await _dashboard.GetOrdersInRangeAsync(q.From, q.To, q.SessionId, ct);
        var completed = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .ToList();

        // ── 2. Período anterior ───────────────────────────────────────────────
        var span = q.To - q.From;
        var prevOrders = await _dashboard.GetOrdersInRangeAsync(
            q.From - span, q.From, null, ct);
        var prevCompleted = prevOrders
            .Where(o => o.Status == OrderStatus.Completed)
            .ToList();

        // ── 3. Totales base ───────────────────────────────────────────────────
        var currentTotal = completed.Sum(o => o.Total);
        var previousTotal = prevCompleted.Sum(o => o.Total);
        var changePercent = previousTotal == 0
            ? 100m
            : Math.Round((currentTotal - previousTotal) / previousTotal * 100, 1);

        // ── 4. Métodos de pago ────────────────────────────────────────────────
        var totalCash = completed
            .Where(o => o.PaymentMethod == PaymentMethod.Cash)
            .Sum(o => o.Total);
        var totalCard = completed
            .Where(o => o.PaymentMethod == PaymentMethod.Card)
            .Sum(o => o.Total);
        var totalTransfer = completed
            .Where(o => o.PaymentMethod == PaymentMethod.Transfer)
            .Sum(o => o.Total);
        var totalCredit = completed
            .Where(o => o.PaymentMethod == PaymentMethod.PayLater)
            .Sum(o => o.Total);

        // ── 5. Ventas por día ─────────────────────────────────────────────────
        var salesByDay = completed
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesDto(
                g.Key,
                Math.Round(g.Sum(o => o.Total), 2),
                g.Count()))
            .ToList();

        // ── 6. Top productos ──────────────────────────────────────────────────
        var allItems = completed.SelectMany(o => o.Items).ToList();
        var topProducts = allItems
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto(
                g.Key,
                Math.Round(g.Sum(i => i.Quantity), 2),
                Math.Round(g.Sum(i => i.LineTotal), 2),
                g.Select(i => i.OrderId).Distinct().Count()))
            .OrderByDescending(p => p.TotalRevenue)
            .Take(10)
            .ToList();

        // ── 7. Top clientes ───────────────────────────────────────────────────
        var topCustomers = completed
            .Where(o => o.CustomerName != null)
            .GroupBy(o => o.CustomerName!)
            .Select(g => new TopCustomerDto(
                g.Key,
                Math.Round(g.Sum(o => o.Total), 2),
                g.Count(),
                g.Max(o => o.CreatedAt)))
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .ToList();

        // ── 8. Métricas extra ─────────────────────────────────────────────────

        // Ticket más alto del período
        var maxTicket = completed.Any()
            ? completed.Max(o => o.Total)
            : 0m;

        // Clientes únicos (excluye Público General)
        var uniqueCustomers = completed
            .Where(o => !string.IsNullOrEmpty(o.CustomerName)
                     && o.CustomerName != "Público General")
            .Select(o => o.CustomerName)
            .Distinct()
            .Count();

        // Promedio de items por orden
        var avgItemsPerOrder = completed.Count > 0
            ? Math.Round((double)allItems.Count / completed.Count, 1)
            : 0.0;

        // Hora pico (hora con más ingresos)
        var peakHour = completed.Any()
            ? completed
                .GroupBy(o => o.CreatedAt.Hour)
                .OrderByDescending(g => g.Sum(o => o.Total))
                .Select(g => (int?)g.Key)
                .FirstOrDefault()
            : null;

        // Producto estrella
        var bestProduct = topProducts.FirstOrDefault();

        var totalPendingDebt = await _debts.GetTotalPendingAllAsync(ct);

        // ── 9. Resultado ──────────────────────────────────────────────────────
        return Result.Ok(new DashboardDto(
            TotalSales: Math.Round(currentTotal, 2),
            TotalOrders: completed.Count,
            AverageTicket: completed.Count > 0
                                          ? Math.Round(currentTotal / completed.Count, 2)
                                          : 0,
            TotalDiscounts: Math.Round(completed.Sum(o => o.DiscountAmount), 2),
            TotalCash: Math.Round(totalCash, 2),
            TotalCard: Math.Round(totalCard, 2),
            TotalTransfer: Math.Round(totalTransfer, 2),
            TotalCreditSales: Math.Round(totalCredit, 2),
            TotalPendingDebt: Math.Round(totalPendingDebt, 2),
            MaxTicket: Math.Round(maxTicket, 2),
            UniqueCustomers: uniqueCustomers,
            AvgItemsPerOrder: avgItemsPerOrder,
            PeakHour: peakHour,
            BestSellingProductName: bestProduct?.ProductName,
            BestSellingProductRevenue: bestProduct != null
                                          ? Math.Round(bestProduct.TotalRevenue, 2)
                                          : 0,
            SalesByDay: salesByDay,
            TopProducts: topProducts,
            TopCustomers: topCustomers,
            Comparison: new PeriodComparisonDto(
                currentTotal,
                previousTotal,
                changePercent,
                completed.Count,
                prevCompleted.Count),
            From: q.From,
            To: q.To,
            SessionId: q.SessionId
        ));
    }
}