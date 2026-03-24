using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Commands;

public record SetCustomerProductPriceCommand(
    Guid CustomerId,
    Guid ProductId,
    decimal CustomPrice
) : IRequest<Result<bool>>;

public class SetCustomerProductPriceHandler
    : IRequestHandler<SetCustomerProductPriceCommand, Result<bool>>
{
    private readonly ICustomerProductPriceRepository _prices;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;

    public SetCustomerProductPriceHandler(
        ICustomerProductPriceRepository prices,
        ICustomerRepository customers,
        IProductRepository products)
    {
        _prices = prices;
        _customers = customers;
        _products = products;
    }

    public async Task<Result<bool>> Handle(SetCustomerProductPriceCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is null) return Result.Fail<bool>("Customer not found.");

        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Fail<bool>("Product not found.");

        try
        {
            var existing = await _prices.GetAsync(cmd.CustomerId, cmd.ProductId, ct);
            if (existing is not null)
                existing.UpdatePrice(cmd.CustomPrice);
            else
            {
                var newPrice = CustomerProductPrice.Create(cmd.CustomerId, cmd.ProductId, cmd.CustomPrice);
                await _prices.UpsertAsync(newPrice, ct);
            }

            await _prices.SaveChangesAsync(ct);
            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}