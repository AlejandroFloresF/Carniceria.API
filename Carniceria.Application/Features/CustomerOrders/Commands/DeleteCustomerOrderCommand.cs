using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Commands;

public record DeleteCustomerOrderCommand(Guid OrderId) : IRequest<Result<bool>>;

public class DeleteCustomerOrderHandler : IRequestHandler<DeleteCustomerOrderCommand, Result<bool>>
{
    private readonly ICustomerOrderRepository _orders;
    public DeleteCustomerOrderHandler(ICustomerOrderRepository orders) => _orders = orders;

    public async Task<Result<bool>> Handle(DeleteCustomerOrderCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Fail<bool>("Customer order not found.");
        await _orders.DeleteAsync(cmd.OrderId, ct);
        return Result.Ok(true);
    }
}
