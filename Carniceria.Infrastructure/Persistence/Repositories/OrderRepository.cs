using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    public async Task AddAsync(Order order, CancellationToken ct = default) { await _db.Orders.AddAsync(order, ct); await _db.SaveChangesAsync(ct); }
}
