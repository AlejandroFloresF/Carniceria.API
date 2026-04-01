using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _db;
    public SessionRepository(AppDbContext db) => _db = db;
    public Task SaveChangesAsync(CancellationToken ct = default) =>
    _db.SaveChangesAsync(ct);
    public Task<CashierSession?> GetByIdAsync(Guid id, CancellationToken ct = default) => _db.CashierSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
    public Task<CashierSession?> GetOpenSessionAsync(string cashierName, CancellationToken ct = default) => _db.CashierSessions.FirstOrDefaultAsync(s => s.CashierName == cashierName && s.Status == SessionStatus.Open, ct);
    public async Task AddAsync(CashierSession session, CancellationToken ct = default) { await _db.CashierSessions.AddAsync(session, ct); await _db.SaveChangesAsync(ct); }
    public Task<List<Order>> GetOrdersAsync(Guid sessionId, CancellationToken ct = default) => _db.Orders.Include(o => o.Items).Where(o => o.CashierSessionId == sessionId).ToListAsync(ct);
    public Task<List<string>> GetDistinctCashiersAsync(CancellationToken ct = default) =>
    _db.CashierSessions
       .Select(s => s.CashierName)
       .Distinct()
       .OrderBy(n => n)
       .ToListAsync(ct);
    public Task<CashierSession?> GetOpenSessionByCashierAsync(
    string cashierName, CancellationToken ct = default) =>
    _db.CashierSessions
       .Where(s => s.CashierName == cashierName
                && s.Status == SessionStatus.Open)
       .OrderByDescending(s => s.OpenedAt)
       .FirstOrDefaultAsync(ct);
    public Task<CashierSession?> GetAnyOpenSessionAsync(CancellationToken ct = default) =>
        _db.CashierSessions
           .Where(s => s.Status == SessionStatus.Open)
           .OrderByDescending(s => s.OpenedAt)
           .FirstOrDefaultAsync(ct);
}
