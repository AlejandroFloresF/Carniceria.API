using Carniceria.Application.Common;
using Carniceria.Application.Features.CustomerOrders;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Commands;

public record UpdateCustomerOrderCommand(
    Guid OrderId,
    string Recurrence,
    DateTime NextDeliveryDate,
    List<CreateCustomerOrderItemInput> Items,
    string? Notes
) : IRequest<Result<CustomerOrderDto>>;

public class UpdateCustomerOrderHandler : IRequestHandler<UpdateCustomerOrderCommand, Result<CustomerOrderDto>>
{
    private readonly ICustomerOrderRepository _orders;
    private readonly IProductRepository _products;

    public UpdateCustomerOrderHandler(ICustomerOrderRepository orders, IProductRepository products)
    {
        _orders   = orders;
        _products = products;
    }

    public async Task<Result<CustomerOrderDto>> Handle(UpdateCustomerOrderCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Fail<CustomerOrderDto>("Customer order not found.");
        if (!cmd.Items.Any()) return Result.Fail<CustomerOrderDto>("Order must have at least one item.");

        order.Update(cmd.Recurrence, cmd.NextDeliveryDate.ToUniversalTime(), cmd.Notes);

        var newItems = new List<(Guid, string, decimal)>();
        foreach (var item in cmd.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null) return Result.Fail<CustomerOrderDto>($"Product {item.ProductId} not found.");
            newItems.Add((product.Id, product.Name, item.QuantityKg));
        }
        order.ReplaceItems(newItems);

        await _orders.UpdateWithItemReplacementAsync(order, ct);
        return Result.Ok(order.ToDto(new Dictionary<Guid, decimal>()));
    }
}
