using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid ProductId) : IRequest<Result<bool>>;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly IProductRepository _products;
    public DeleteProductHandler(IProductRepository products) => _products = products;

    public async Task<Result<bool>> Handle(DeleteProductCommand cmd, CancellationToken ct)
    {
        if (!await _products.ExistsAsync(cmd.ProductId, ct))
            return Result.Fail<bool>("Product not found.");
        await _products.DeleteAsync(cmd.ProductId, ct);
        return Result.Ok(true);
    }
}
