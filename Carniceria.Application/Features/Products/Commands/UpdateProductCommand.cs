using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Products.Commands;

public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Category,
    decimal Price,
    string Unit,
    string? Barcode = null
) : IRequest<Result<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly IProductRepository _products;
    public UpdateProductHandler(IProductRepository products) => _products = products;

    public async Task<Result<bool>> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Fail<bool>("Product not found.");
        try
        {
            product.Update(cmd.Name, cmd.Category, cmd.Price, cmd.Unit, cmd.Barcode);
            await _products.SaveChangesAsync(ct);
            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}
