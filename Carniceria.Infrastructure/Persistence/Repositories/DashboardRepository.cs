using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;
    public DashboardRepository(AppDbContext db) => _db = db;

    public Task<List<Order>> GetOrdersInRangeAsync(
    DateTime from, DateTime to, Guid? sessionId, CancellationToken ct = default)
    {
        var q = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to
                     && o.Status == OrderStatus.Completed); 

        if (sessionId.HasValue)
            q = q.Where(o => o.CashierSessionId == sessionId.Value);

        return q.ToListAsync(ct);
    }
}