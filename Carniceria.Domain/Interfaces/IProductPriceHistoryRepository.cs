using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface IProductPriceHistoryRepository
{
    Task AddAsync(ProductPriceHistory entry, CancellationToken ct);
    Task<List<ProductPriceHistory>> GetByProductAsync(Guid productId, CancellationToken ct);
}
