using Carniceria.Application.Common;
using Carniceria.Domain.Entities;

namespace Carniceria.Application.Features.CustomerOrders;

public static class CustomerOrderExtensions
{
    private const int UpcomingDays = 3;

    /// <param name="stockMap">Current stock per product (from ProductRepository).</param>
    public static CustomerOrderDto ToDto(
        this CustomerOrder order,
        Dictionary<Guid, decimal> stockMap)
    {
        var today = DateTime.UtcNow.Date;
        var isUpcoming = order.NextDeliveryDate.Date <= today.AddDays(UpcomingDays);

        var itemDtos = order.Items
            .Select(i => new CustomerOrderItemDto(i.ProductId, i.ProductName, i.QuantityKg))
            .ToList();

        var hasShortage = order.Items.Any(i =>
            stockMap.TryGetValue(i.ProductId, out var stock) && stock < i.QuantityKg);

        return new CustomerOrderDto(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.Recurrence,
            order.NextDeliveryDate,
            itemDtos,
            order.Notes,
            order.IsActive,
            isUpcoming,
            hasShortage);
    }
}
