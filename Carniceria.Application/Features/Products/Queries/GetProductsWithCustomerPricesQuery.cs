using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

public record ProductWithPriceDto(
    Guid Id, string Name, string Category,
    decimal GeneralPrice,
    decimal EffectivePrice,  // precio que aplica para este cliente
    string Unit, decimal StockKg,
    bool HasCustomPrice
);

public record GetProductsWithCustomerPricesQuery(Guid CustomerId)
    : IRequest<Result<List<ProductWithPriceDto>>>;

public class GetProductsWithCustomerPricesHandler
    : IRequestHandler<GetProductsWithCustomerPricesQuery, Result<List<ProductWithPriceDto>>>
{
    private readonly IProductRepository _products;
    private readonly ICustomerProductPriceRepository _prices;

    public GetProductsWithCustomerPricesHandler(
        IProductRepository products,
        ICustomerProductPriceRepository prices)
    {
        _products = products;
        _prices = prices;
    }

    public async Task<Result<List<ProductWithPriceDto>>> Handle(
        GetProductsWithCustomerPricesQuery q, CancellationToken ct)
    {
        var products = await _products.SearchAsync(null, ct);
        var customPrices = await _prices.GetByCustomerAsync(q.CustomerId, ct);
        var priceMap = customPrices.ToDictionary(p => p.ProductId);

        var result = products.Select(p =>
        {
            var hasCustom = priceMap.TryGetValue(p.Id, out var cp);
            var effectivePrice = hasCustom ? cp!.CustomPrice : p.PricePerUnit;
            return new ProductWithPriceDto(
                p.Id, p.Name, p.Category,
                p.PricePerUnit, effectivePrice,
                p.Unit, p.StockKg, hasCustom);
        }).ToList();

        return Result.Ok(result);
    }
}