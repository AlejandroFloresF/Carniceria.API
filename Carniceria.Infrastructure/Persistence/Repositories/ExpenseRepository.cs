using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Carniceria.Infrastructure.Persistence.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _db;
    public ExpenseRepository(AppDbContext db) => _db = db;

    public Task<List<ScheduledExpense>> GetScheduledActiveAsync(CancellationToken ct) =>
        _db.ScheduledExpenses.OrderBy(e => e.NextDueDate).ToListAsync(ct);

    public Task<ScheduledExpense?> GetScheduledByIdAsync(Guid id, CancellationToken ct) =>
        _db.ScheduledExpenses.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddScheduledAsync(ScheduledExpense expense, CancellationToken ct)
    {
        await _db.ScheduledExpenses.AddAsync(expense, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteScheduledAsync(ScheduledExpense expense, CancellationToken ct)
    {
        _db.ScheduledExpenses.Remove(expense);
        await _db.SaveChangesAsync(ct);
    }

    public Task<decimal> GetApprovedTotalBySessionAsync(Guid sessionId, CancellationToken ct) =>
        _db.ExpenseRequests
           .Where(r => r.SessionId == sessionId && r.Status == "Approved")
           .SumAsync(r => r.Amount, ct);

    public Task<List<ExpenseRequest>> GetRequestsAsync(string? status, string? requestedBy, CancellationToken ct)
    {
        var q = _db.ExpenseRequests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))      q = q.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(requestedBy)) q = q.Where(r => r.RequestedBy == requestedBy);
        return q.OrderByDescending(r => r.RequestedAt).ToListAsync(ct);
    }

    public Task<ExpenseRequest?> GetRequestByIdAsync(Guid id, CancellationToken ct) =>
        _db.ExpenseRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddRequestAsync(ExpenseRequest request, CancellationToken ct)
    {
        await _db.ExpenseRequests.AddAsync(request, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
