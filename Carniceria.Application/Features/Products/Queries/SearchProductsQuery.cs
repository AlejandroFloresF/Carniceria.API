using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Products.Queries;
public record SearchProductsQuery(string? Search) : IRequest<Result<List<ProductDto>>>;
public class SearchProductsHandler : IRequestHandler<SearchProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductRepository _products;
    public SearchProductsHandler(IProductRepository products) => _products = products;
    public async Task<Result<List<ProductDto>>> Handle(SearchProductsQuery q, CancellationToken ct)
    {
        var products = await _products.SearchAsync(q.Search, ct);
        var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Category, p.PricePerUnit, p.Unit, p.StockKg)).ToList();
        return Result.Ok(dtos);
    }
}
