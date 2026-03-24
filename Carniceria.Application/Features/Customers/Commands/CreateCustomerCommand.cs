using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Commands;

public record CreateCustomerCommand(
    string Name,
    string? Phone,
    string? Address,          // ← era Email
    decimal DiscountPercent,
    string color = "#6366f1",
    string? emoji = null
) : IRequest<Result<CustomerDto>>;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    public CreateCustomerHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        try
        {
            var customer = Customer.Create(cmd.Name, cmd.Phone, cmd.Address, cmd.DiscountPercent, cmd.color, cmd.emoji);
            await _customers.AddAsync(customer, ct);
            return Result.Ok(new CustomerDto(
                customer.Id, customer.Name, customer.Phone,
                customer.Address, customer.DiscountPercent, 0, customer.Color, customer.Emoji));
        }
        catch (DomainException ex) { return Result.Fail<CustomerDto>(ex.Message); }
    }
}