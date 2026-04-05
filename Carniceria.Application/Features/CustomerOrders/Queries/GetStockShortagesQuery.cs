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

    public GetStockShortagesHandler(ICustomerOrderRepository orders, IProductRepository products)
    {
        _orders   = orders;
        _products = products;
    }

    public async Task<Result<List<StockShortageOrderDto>>> Handle(GetStockShortagesQuery req, CancellationToken ct)
    {
        var today      = DateTime.UtcNow.Date;
        var cutoff     = today.AddDays(req.AlertDaysAhead);
        var active     = await _orders.GetActiveAsync(ct);
        var upcoming   = active.Where(o => o.NextDeliveryDate.Date <= cutoff).ToList();
        var reservedMap = await _orders.GetTotalReservedByProductAsync(ct);

        // Load current stock for all relevant products
        var productIds = upcoming.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct();
        var stockMap   = new Dictionary<Guid, decimal>();
        foreach (var pid in productIds)
        {
            var product = await _products.GetByIdAsync(pid, ct);
            if (product is not null) stockMap[pid] = product.StockKg;
        }

        var shortages = new List<StockShortageOrderDto>();
        foreach (var order in upcoming)
        {
            var shortageItems = order.Items
                .Select(item =>
                {
                    var stock    = stockMap.GetValueOrDefault(item.ProductId, 0m);
                    var reserved = reservedMap.GetValueOrDefault(item.ProductId, 0m);
                    // Available = stock minus what ALL orders need
                    // If stock < what this order alone needs it's a shortage
                    return stock < item.QuantityKg
                        ? new StockShortageItemDto(
                            item.ProductId, item.ProductName,
                            item.QuantityKg, stock)
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
