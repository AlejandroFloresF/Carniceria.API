using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    public Task<List<Product>> SearchAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Products.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.ToLower().Contains(search.ToLower()));
        return q.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);
    }
    public Task<List<Product>> GetAllIncludingInactiveAsync(CancellationToken ct = default) =>
        _db.Products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(ct);
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is not null) { _db.Products.Remove(product); await _db.SaveChangesAsync(ct); }
    }
    public async Task AddAsync(Product product, CancellationToken ct = default) { await _db.Products.AddAsync(product, ct); await _db.SaveChangesAsync(ct); }
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => _db.Products.AnyAsync(p => p.Id == id, ct);
    public Task SaveChangesAsync(CancellationToken ct = default) =>
    _db.SaveChangesAsync(ct);
}
