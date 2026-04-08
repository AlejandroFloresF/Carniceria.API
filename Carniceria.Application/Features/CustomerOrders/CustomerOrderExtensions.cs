using Carniceria.Application.Common;
using Carniceria.Domain.Entities;

namespace Carniceria.Application.Features.CustomerOrders;

public static class CustomerOrderExtensions
{
    private const int UpcomingDays = 3;

    /// <param name="stockMap">Current stock per product.</param>
    /// <param name="minimumMap">Configured minimum stock per product (StockAlert). Null = ignore minimums.</param>
    public static CustomerOrderDto ToDto(
        this CustomerOrder order,
        Dictionary<Guid, decimal> stockMap,
        Dictionary<Guid, decimal>? minimumMap = null)
    {
        var today = DateTime.UtcNow.Date;
        var isUpcoming = order.NextDeliveryDate.Date <= today.AddDays(UpcomingDays);

        var itemDtos = order.Items
            .Select(i => new CustomerOrderItemDto(i.ProductId, i.ProductName, i.QuantityKg))
            .ToList();

        // Shortage = current stock minus minimum buffer is not enough for this order.
        // Other pending orders are NOT subtracted — stock is the real source of truth.
        var hasShortage = order.Items.Any(i =>
        {
            if (!stockMap.TryGetValue(i.ProductId, out var stock)) return false;
            var minimum   = minimumMap?.GetValueOrDefault(i.ProductId, 0m) ?? 0m;
            var available = stock - minimum;
            return available < i.QuantityKg;
        });

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
