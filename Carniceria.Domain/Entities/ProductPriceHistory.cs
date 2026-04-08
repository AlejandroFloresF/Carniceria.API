using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class ProductPriceHistory : BaseEntity
{
    public Guid    ProductId   { get; private set; }
    public string  ProductName { get; private set; } = string.Empty;
    public decimal OldPrice    { get; private set; }
    public decimal NewPrice    { get; private set; }

    private ProductPriceHistory() { }

    public static ProductPriceHistory Record(Guid productId, string productName, decimal oldPrice, decimal newPrice) =>
        new()
        {
            ProductId   = productId,
            ProductName = productName,
            OldPrice    = oldPrice,
            NewPrice    = newPrice,
        };
}
