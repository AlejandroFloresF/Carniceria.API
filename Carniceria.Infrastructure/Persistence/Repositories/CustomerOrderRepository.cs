using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class CustomerOrderRepository : ICustomerOrderRepository
{
    private readonly AppDbContext _db;
    public CustomerOrderRepository(AppDbContext db) => _db = db;

    public Task<List<CustomerOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        _db.CustomerOrders
           .Include(o => o.Items)
           .Where(o => o.CustomerId == customerId && o.IsActive)
           .OrderBy(o => o.NextDeliveryDate)
           .ToListAsync(ct);

    public Task<CustomerOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.CustomerOrders
           .Include(o => o.Items)
           .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<List<CustomerOrder>> GetActiveAsync(CancellationToken ct = default) =>
        _db.CustomerOrders
           .Include(o => o.Items)
           .Where(o => o.IsActive)
           .ToListAsync(ct);

    public async Task<Dictionary<Guid, decimal>> GetTotalReservedByProductAsync(CancellationToken ct = default)
    {
        var active = await _db.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.IsActive)
            .ToListAsync(ct);

        return active
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantityKg));
    }

    public async Task<Dictionary<Guid, decimal>> GetReservedByProductForCustomerAsync(
        Guid customerId, CancellationToken ct = default)
    {
        var active = await _db.CustomerOrders
            .Include(o => o.Items)
            .Where(o => o.IsActive && o.CustomerId == customerId)
            .ToListAsync(ct);

        return active
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantityKg));
    }

    public async Task AddAsync(CustomerOrder order, CancellationToken ct = default)
    {
        _db.CustomerOrders.Add(order);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.CustomerOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is not null) _db.CustomerOrders.Remove(order);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public async Task UpdateWithItemReplacementAsync(CustomerOrder order, CancellationToken ct = default)
    {
        // 1. Delete old items directly — avoids EF orphan-tracking issues
        var oldItems = await _db.CustomerOrderItems
            .Where(i => i.CustomerOrderId == order.Id)
            .ToListAsync(ct);
        if (oldItems.Count > 0)
        {
            _db.CustomerOrderItems.RemoveRange(oldItems);
            await _db.SaveChangesAsync(ct);
        }

        // 2. Detach & re-attach order so EF picks up the new items
        _db.ChangeTracker.Clear();
        _db.CustomerOrders.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}
