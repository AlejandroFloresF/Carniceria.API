using Carniceria.Application.Common;
using Carniceria.Application.Features.CustomerOrders;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Queries;

/// <summary>Returns all active orders whose NextDeliveryDate is today or overdue.</summary>
public record GetTodayOrdersQuery : IRequest<Result<List<CustomerOrderDto>>>;

public class GetTodayOrdersHandler : IRequestHandler<GetTodayOrdersQuery, Result<List<CustomerOrderDto>>>
{
    private readonly ICustomerOrderRepository _orders;
    private readonly IProductRepository _products;

    public GetTodayOrdersHandler(ICustomerOrderRepository orders, IProductRepository products)
    {
        _orders   = orders;
        _products = products;
    }

    public async Task<Result<List<CustomerOrderDto>>> Handle(GetTodayOrdersQuery req, CancellationToken ct)
    {
        var today  = DateTime.UtcNow.Date;
        var active = await _orders.GetActiveAsync(ct);
        var due    = active.Where(o => o.NextDeliveryDate.Date <= today).ToList();

        var productIds = due.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct();
        var stockMap   = new Dictionary<Guid, decimal>();
        foreach (var pid in productIds)
        {
            var p = await _products.GetByIdAsync(pid, ct);
            if (p is not null) stockMap[pid] = p.StockKg;
        }

        return Result.Ok(due.Select(o => o.ToDto(stockMap)).ToList());
    }
}
