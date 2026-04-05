using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.CustomerOrders.Commands;

public record FulfillCustomerOrderCommand(Guid OrderId) : IRequest<Result<bool>>;

public class FulfillCustomerOrderHandler : IRequestHandler<FulfillCustomerOrderCommand, Result<bool>>
{
    private readonly ICustomerOrderRepository _orders;
    public FulfillCustomerOrderHandler(ICustomerOrderRepository orders) => _orders = orders;

    public async Task<Result<bool>> Handle(FulfillCustomerOrderCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Fail<bool>("Customer order not found.");

        if (order.Recurrence == "None")
            order.Deactivate();          // one-time → close it
        else
            order.AdvanceDeliveryDate(); // periodic → move to next date

        await _orders.SaveChangesAsync(ct);
        return Result.Ok(true);
    }
}
