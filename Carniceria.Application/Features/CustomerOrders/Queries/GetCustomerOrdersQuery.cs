using Carniceria.Application.Common;
using Carniceria.Application.Features.CustomerOrders;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Queries;

public record GetCustomerOrdersQuery(Guid CustomerId) : IRequest<Result<List<CustomerOrderDto>>>;

public class GetCustomerOrdersHandler : IRequestHandler<GetCustomerOrdersQuery, Result<List<CustomerOrderDto>>>
{
    private readonly ICustomerOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public GetCustomerOrdersHandler(
        ICustomerOrderRepository orders,
        IProductRepository products,
        IInventoryRepository inventory)
    {
        _orders    = orders;
        _products  = products;
        _inventory = inventory;
    }

    public async Task<Result<List<CustomerOrderDto>>> Handle(GetCustomerOrdersQuery req, CancellationToken ct)
    {
        var orders = await _orders.GetByCustomerAsync(req.CustomerId, ct);

        var productIds = orders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var stockMap   = new Dictionary<Guid, decimal>();
        var minimumMap = new Dictionary<Guid, decimal>();
        foreach (var pid in productIds)
        {
            var product = await _products.GetByIdAsync(pid, ct);
            if (product is not null) stockMap[pid] = product.StockKg;

            var alert = await _inventory.GetAlertByProductAsync(pid, ct);
            if (alert is not null) minimumMap[pid] = alert.MinimumStockKg;
        }

        var dtos = orders.Select(o => o.ToDto(stockMap, minimumMap)).ToList();
        return Result.Ok(dtos);
    }
}
