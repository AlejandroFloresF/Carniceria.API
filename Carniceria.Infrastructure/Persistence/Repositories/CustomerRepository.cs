using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

    public Task<List<Customer>> SearchAsync(string? search, CancellationToken ct = default)
    {
        var q = _db.Customers.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.ToLower().Contains(search.ToLower()));

        return q
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await _db.Customers.AddAsync(customer, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
    public Task<decimal> GetTotalPendingAllAsync(CancellationToken ct = default) =>
    _db.CustomerDebts
       .Where(d => d.Status == DebtStatus.Pending)
       .SumAsync(d => d.Amount, ct);
}