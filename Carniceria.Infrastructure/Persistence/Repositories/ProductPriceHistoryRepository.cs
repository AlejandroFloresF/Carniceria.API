using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class ProductPriceHistoryRepository : IProductPriceHistoryRepository
{
    private readonly AppDbContext _db;
    public ProductPriceHistoryRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ProductPriceHistory entry, CancellationToken ct)
    {
        await _db.ProductPriceHistory.AddAsync(entry, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<ProductPriceHistory>> GetByProductAsync(Guid productId, CancellationToken ct) =>
        _db.ProductPriceHistory
           .Where(h => h.ProductId == productId)
           .OrderByDescending(h => h.CreatedAt)
           .ToListAsync(ct);
}
