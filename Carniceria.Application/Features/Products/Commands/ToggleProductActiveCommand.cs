using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Products.Commands;

public record ToggleProductActiveCommand(Guid ProductId) : IRequest<Result<bool>>;

public class ToggleProductActiveHandler : IRequestHandler<ToggleProductActiveCommand, Result<bool>>
{
    private readonly IProductRepository _products;
    public ToggleProductActiveHandler(IProductRepository products) => _products = products;

    public async Task<Result<bool>> Handle(ToggleProductActiveCommand cmd, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Fail<bool>("Product not found.");
        if (product.IsActive) product.Deactivate(); else product.Activate();
        await _products.SaveChangesAsync(ct);
        return Result.Ok(true);
    }
}
