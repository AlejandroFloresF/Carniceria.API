using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Commands;

public record DeleteCustomerCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, Result<bool>>
{
    private readonly ICustomerRepository _customers;
    public DeleteCustomerHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<Result<bool>> Handle(DeleteCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(cmd.Id, ct);
        if (customer is null) return Result.Fail<bool>("Customer not found.");

        customer.Deactivate();
        await _customers.SaveChangesAsync(ct);
        return Result.Ok(true);
    }
}