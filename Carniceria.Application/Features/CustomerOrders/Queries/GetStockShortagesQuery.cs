using Carniceria.Application.Common;
using Carniceria.Application.Features.CustomerOrders;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Queries;

public record GetStockShortagesQuery(int AlertDaysAhead = 3) : IRequest<Result<List<StockShortageOrderDto>>>;

public class GetStockShortagesHandler : IRequestHandler<GetStockShortagesQuery, Result<List<StockShortageOrderDto>>>
{
    private readonly ICustomerOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public GetStockShortagesHandler(
        ICustomerOrderRepository orders,
        IProductRepository products,
        IInventoryRepository inventory)
    {
        _orders    = orders;
        _products  = products;
        _inventory = inventory;
    }

    public async Task<Result<List<StockShortageOrderDto>>> Handle(GetStockShortagesQuery req, CancellationToken ct)
    {
        var today      = DateTime.UtcNow.Date;
        var cutoff     = today.AddDays(req.AlertDaysAhead);
        var active   = await _orders.GetActiveAsync(ct);
        var upcoming = active.Where(o => o.NextDeliveryDate.Date <= cutoff).ToList();

        // Current stock + configured minimum per product
        var productIds = upcoming.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var stockMap   = new Dictionary<Guid, decimal>();
        var minimumMap = new Dictionary<Guid, decimal>();
        foreach (var pid in productIds)
        {
            var product = await _products.GetByIdAsync(pid, ct);
            if (product is not null) stockMap[pid] = product.StockKg;

            var alert = await _inventory.GetAlertByProductAsync(pid, ct);
            if (alert is not null) minimumMap[pid] = alert.MinimumStockKg;
        }

        var shortages = new List<StockShortageOrderDto>();
        foreach (var order in upcoming)
        {
            var shortageItems = order.Items
                .Select(item =>
                {
                    var stock     = stockMap.GetValueOrDefault(item.ProductId, 0m);
                    var minimum   = minimumMap.GetValueOrDefault(item.ProductId, 0m);
                    // Available = stock minus minimum buffer. Other orders don't subtract
                    // because stock is the real source of truth, not speculative reservations.
                    var available = stock - minimum;

                    return available < item.QuantityKg
                        ? new StockShortageItemDto(
                            item.ProductId, item.ProductName,
                            item.QuantityKg, Math.Max(0m, available))
                        : null;
                })
                .Where(x => x is not null)
                .Cast<StockShortageItemDto>()
                .ToList();

            if (shortageItems.Any())
            {
                shortages.Add(new StockShortageOrderDto(
                    order.Id, order.CustomerId, order.CustomerName,
                    order.NextDeliveryDate, order.Recurrence, shortageItems));
            }
        }

        return Result.Ok(shortages);
    }
}
