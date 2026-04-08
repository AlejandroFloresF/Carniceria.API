using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class CashWithdrawalRepository : ICashWithdrawalRepository
{
    private readonly AppDbContext _db;
    public CashWithdrawalRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(CashWithdrawal withdrawal, CancellationToken ct)
    {
        await _db.CashWithdrawals.AddAsync(withdrawal, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<CashWithdrawal>> GetBySessionAsync(Guid sessionId, CancellationToken ct) =>
        _db.CashWithdrawals
           .Where(w => w.SessionId == sessionId)
           .OrderBy(w => w.CreatedAt)
           .ToListAsync(ct);
}
