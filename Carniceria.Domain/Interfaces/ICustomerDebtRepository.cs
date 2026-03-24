using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface ICustomerDebtRepository
{
    Task<List<CustomerDebt>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerDebt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<decimal> GetTotalPendingAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(CustomerDebt debt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<decimal> GetTotalPendingAllAsync(CancellationToken ct = default);

}