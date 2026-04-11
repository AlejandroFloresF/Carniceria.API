using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> SearchAsync(string? search, CancellationToken ct = default);
    Task<List<Product>> GetAllIncludingInactiveAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    /// <summary>
    /// Deducts stock atomically at DB level (UPDATE … WHERE StockKg >= qty).
    /// Returns false if stock was insufficient (concurrent sale depleted it first).
    /// </summary>
    /// <summary>Looks up an active product by its barcode/PLU string (exact match).</summary>
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    /// <summary>
    /// Deducts stock atomically at DB level (UPDATE … WHERE StockKg >= qty).
    /// Returns false if stock was insufficient (concurrent sale depleted it first).
    /// </summary>
    Task<bool> DeductStockAtomicAsync(Guid productId, decimal qty, CancellationToken ct = default);
}
