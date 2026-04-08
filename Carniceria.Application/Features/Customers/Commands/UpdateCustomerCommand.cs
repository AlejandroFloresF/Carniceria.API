using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Commands;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    decimal DiscountPercent,
    string color = "#6366f1",
    string? emoji = null,
    string? notes = null
) : IRequest<Result<CustomerDto>>;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    public UpdateCustomerHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(cmd.Id, ct);
        if (customer is null) return Result.Fail<CustomerDto>("Customer not found.");

        try
        {
            customer.Update(cmd.Name, cmd.Phone, cmd.Address, cmd.DiscountPercent, cmd.color, cmd.emoji, cmd.notes);
            await _customers.SaveChangesAsync(ct);
            return Result.Ok(new CustomerDto(
                customer.Id, customer.Name, customer.Phone,
                customer.Address, customer.DiscountPercent, 0, customer.Color, customer.Emoji, customer.Notes));
        }
        catch (DomainException ex) { return Result.Fail<CustomerDto>(ex.Message); }
    }
}