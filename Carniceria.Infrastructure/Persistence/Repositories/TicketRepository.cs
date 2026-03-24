using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;
public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;
    public TicketRepository(AppDbContext db) => _db = db;
    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default) => 
        _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task<Ticket?> GetByFolioAsync(string folio, CancellationToken ct = default) => 
        _db.Tickets.FirstOrDefaultAsync(t => t.Folio == folio, ct);
    public async Task AddAsync(Ticket ticket, CancellationToken ct = default) 
        { await _db.Tickets.AddAsync(ticket, ct); await _db.SaveChangesAsync(ct); }
    public async Task<int> GetNextFolioNumberAsync(CancellationToken ct = default) 
        { var count = await _db.Tickets.CountAsync(ct); return count + 1; }
    public Task<Ticket?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Tickets.FirstOrDefaultAsync(t => t.OrderId == orderId, ct);
}
