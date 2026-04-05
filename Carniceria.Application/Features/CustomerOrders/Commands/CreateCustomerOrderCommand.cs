using Carniceria.Application.Common;
using Carniceria.Application.Features.CustomerOrders;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Commands;

public record CreateCustomerOrderItemInput(Guid ProductId, string ProductName, decimal QuantityKg);

public record CreateCustomerOrderCommand(
    Guid CustomerId,
    string Recurrence,
    DateTime NextDeliveryDate,
    List<CreateCustomerOrderItemInput> Items,
    string? Notes
) : IRequest<Result<CustomerOrderDto>>;

public class CreateCustomerOrderHandler : IRequestHandler<CreateCustomerOrderCommand, Result<CustomerOrderDto>>
{
    private readonly ICustomerOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;

    public CreateCustomerOrderHandler(
        ICustomerOrderRepository orders,
        ICustomerRepository customers,
        IProductRepository products)
    {
        _orders    = orders;
        _customers = customers;
        _products  = products;
    }

    public async Task<Result<CustomerOrderDto>> Handle(CreateCustomerOrderCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is null) return Result.Fail<CustomerOrderDto>("Customer not found.");
        if (!cmd.Items.Any()) return Result.Fail<CustomerOrderDto>("Order must have at least one item.");

        var order = CustomerOrder.Create(
            customer.Id, customer.Name,
            cmd.Recurrence, cmd.NextDeliveryDate.ToUniversalTime(), cmd.Notes);

        foreach (var item in cmd.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null) return Result.Fail<CustomerOrderDto>($"Product {item.ProductId} not found.");
            try { order.AddItem(product.Id, product.Name, item.QuantityKg); }
            catch (DomainException ex) { return Result.Fail<CustomerOrderDto>(ex.Message); }
        }

        await _orders.AddAsync(order, ct);
        return Result.Ok(order.ToDto(new Dictionary<Guid, decimal>()));
    }
}
