using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Inventory.Queries;

public record GetStockStatusQuery : IRequest<Result<List<StockStatusDto>>>;

public class GetStockStatusHandler : IRequestHandler<GetStockStatusQuery, Result<List<StockStatusDto>>>
{
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IDashboardRepository _dashboard;

    public GetStockStatusHandler(
        IProductRepository products,
        IInventoryRepository inventory,
        IDashboardRepository dashboard)
    {
        _products = products;
        _inventory = inventory;
        _dashboard = dashboard;
    }

    public async Task<Result<List<StockStatusDto>>> Handle(
        GetStockStatusQuery _, CancellationToken ct)
    {
        var products = await _products.SearchAsync(null, ct);
        var alerts = await _inventory.GetAlertsAsync(ct);

        var from7 = DateTime.UtcNow.AddDays(-7);
        var orders = await _dashboard.GetOrdersInRangeAsync(from7, DateTime.UtcNow, null, ct);
        var waste = await _inventory.GetWasteAsync(null, from7, DateTime.UtcNow, ct);

        var alertMap = alerts.ToDictionary(a => a.ProductId);

        var result = products.Select(p =>
        {
            var soldKg = orders
                .SelectMany(o => o.Items)
                .Where(i => i.ProductId == p.Id)
                .Sum(i => i.Quantity);

            var wasteKg = waste
                .Where(w => w.ProductId == p.Id)
                .Sum(w => w.QuantityKg);

            var minKg = alertMap.TryGetValue(p.Id, out var alert) ? alert.MinimumStockKg : 0;
            var avgDaily = Math.Round(soldKg / 7, 2);

            return new StockStatusDto(
                p.Id, p.Name, p.Category,
                CurrentStockKg: p.StockKg,
                MinimumStockKg: minKg,
                IsBelowMinimum: minKg > 0 && p.StockKg < minKg,
                TotalSoldLast7Days: Math.Round(soldKg, 2),
                TotalWasteLast7Days: Math.Round(wasteKg, 2),
                AverageDailySales: avgDaily,
                SalePrice: p.PricePerUnit
            );
        }).ToList();

        return Result.Ok(result);
    }
}