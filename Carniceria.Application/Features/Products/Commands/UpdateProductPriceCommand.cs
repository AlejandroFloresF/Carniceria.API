using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Products.Commands;

public record UpdateProductPriceCommand(
    Guid ProductId,
    decimal NewPrice
) : IRequest<Result<bool>>;

public class UpdateProductPriceHandler
    : IRequestHandler<UpdateProductPriceCommand, Result<bool>>
{
    private readonly IProductRepository _products;
    public UpdateProductPriceHandler(IProductRepository products) => _products = products;

    public async Task<Result<bool>> Handle(UpdateProductPriceCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Fail<bool>("Product not found.");

        try
        {
            product.UpdatePrice(cmd.NewPrice);
            await _products.SaveChangesAsync(ct);
            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}