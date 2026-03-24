using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class CustomerDebtRepository : ICustomerDebtRepository
{
    private readonly AppDbContext _db;
    public CustomerDebtRepository(AppDbContext db) => _db = db;

    public Task<List<CustomerDebt>> GetByCustomerAsync(
        Guid customerId, CancellationToken ct = default) =>
        _db.CustomerDebts
           .Where(d => d.CustomerId == customerId)
           .OrderByDescending(d => d.CreatedAt)
           .ToListAsync(ct);

    public async Task<CustomerDebt?> GetByIdAsync(
        Guid id, CancellationToken ct = default) =>
        await _db.CustomerDebts.FindAsync(new object[] { id }, ct);

    public async Task AddAsync(
        CustomerDebt debt, CancellationToken ct = default)
    {
        await _db.CustomerDebts.AddAsync(debt, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);

    public Task<decimal> GetTotalPendingAsync(
        Guid customerId, CancellationToken ct = default) =>
        _db.CustomerDebts
           .Where(d => d.CustomerId == customerId
                    && d.Status == DebtStatus.Pending)
           .SumAsync(d => d.Amount, ct);

    // ← método nuevo que faltaba:
    public Task<decimal> GetTotalPendingAllAsync(
        CancellationToken ct = default) =>
        _db.CustomerDebts
           .Where(d => d.Status == DebtStatus.Pending)
           .SumAsync(d => d.Amount, ct);
}