using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Products.Commands;

public record CreateProductCommand(
    string Name,
    string Category,
    decimal Price,
    string Unit
) : IRequest<Result<Guid>>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _products;
    public CreateProductHandler(IProductRepository products) => _products = products;

    public async Task<Result<Guid>> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        try
        {
            var product = Product.Create(cmd.Name, cmd.Category, cmd.Price, cmd.Unit, 0);
            await _products.AddAsync(product, ct);
            return Result.Ok(product.Id);
        }
        catch (DomainException ex) { return Result.Fail<Guid>(ex.Message); }
    }
}
