using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class CustomerProductPriceRepository : ICustomerProductPriceRepository
{
    private readonly AppDbContext _db;
    public CustomerProductPriceRepository(AppDbContext db) => _db = db;

    public Task<List<CustomerProductPrice>> GetByCustomerAsync(
        Guid customerId, CancellationToken ct = default) =>
        _db.CustomerProductPrices
           .Where(p => p.CustomerId == customerId)
           .ToListAsync(ct);

    public Task<CustomerProductPrice?> GetAsync(
        Guid customerId, Guid productId, CancellationToken ct = default) =>
        _db.CustomerProductPrices
           .FirstOrDefaultAsync(
               p => p.CustomerId == customerId && p.ProductId == productId, ct);

    public async Task UpsertAsync(
        CustomerProductPrice price, CancellationToken ct = default)
    {
        var existing = await GetAsync(price.CustomerId, price.ProductId, ct);
        if (existing is null)
            await _db.CustomerProductPrices.AddAsync(price, ct);
        else
            existing.UpdatePrice(price.CustomPrice);
    }

    public async Task DeleteAsync(
        Guid customerId, Guid productId, CancellationToken ct = default)
    {
        var existing = await GetAsync(customerId, productId, ct);
        if (existing is not null)
            _db.CustomerProductPrices.Remove(existing);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}