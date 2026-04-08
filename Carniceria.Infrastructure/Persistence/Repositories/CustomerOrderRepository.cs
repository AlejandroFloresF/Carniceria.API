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
        // 1. Delete old items via direct SQL — bypasses EF tracking conflict
        //    with items already marked Deleted by ReplaceItems().
        await _db.CustomerOrderItems
            .Where(i => i.CustomerOrderId == order.Id)
            .ExecuteDeleteAsync(ct);

        // 2. Clear tracker, then manually set states.
        //    Update() marks ALL entities with non-default keys as Modified,
        //    which causes a concurrency error for the brand-new items.
        //    Instead: attach order as Modified, items as Added.
        _db.ChangeTracker.Clear();
        _db.CustomerOrders.Attach(order);
        _db.Entry(order).State = EntityState.Modified;
        foreach (var item in order.Items)
            _db.Entry(item).State = EntityState.Added;

        await _db.SaveChangesAsync(ct);
    }
}
