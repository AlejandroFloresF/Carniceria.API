using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
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
    private readonly IProductPriceHistoryRepository _history;

    public UpdateProductPriceHandler(IProductRepository products, IProductPriceHistoryRepository history)
    {
        _products = products;
        _history  = history;
    }

    public async Task<Result<bool>> Handle(UpdateProductPriceCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Fail<bool>("Product not found.");

        try
        {
            var oldPrice = product.PricePerUnit;
            product.UpdatePrice(cmd.NewPrice);
            await _products.SaveChangesAsync(ct);

            if (oldPrice != cmd.NewPrice)
            {
                var entry = ProductPriceHistory.Record(product.Id, product.Name, oldPrice, cmd.NewPrice);
                await _history.AddAsync(entry, ct);
            }

            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}