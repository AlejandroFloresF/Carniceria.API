using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Products.Queries;

public record GetProductPriceHistoryQuery(Guid ProductId) : IRequest<Result<List<ProductPriceHistoryDto>>>;

public class GetProductPriceHistoryHandler
    : IRequestHandler<GetProductPriceHistoryQuery, Result<List<ProductPriceHistoryDto>>>
{
    private readonly IProductPriceHistoryRepository _history;
    public GetProductPriceHistoryHandler(IProductPriceHistoryRepository history) => _history = history;

    public async Task<Result<List<ProductPriceHistoryDto>>> Handle(GetProductPriceHistoryQuery q, CancellationToken ct)
    {
        var entries = await _history.GetByProductAsync(q.ProductId, ct);
        var dtos = entries
            .Select(e => new ProductPriceHistoryDto(e.Id, e.OldPrice, e.NewPrice, e.CreatedAt))
            .ToList();
        return Result.Ok(dtos);
    }
}
