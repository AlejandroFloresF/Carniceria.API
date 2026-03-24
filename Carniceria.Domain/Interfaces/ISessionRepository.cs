using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface ISessionRepository
{
    Task<CashierSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CashierSession?> GetOpenSessionAsync(string cashierName, CancellationToken ct = default);
    Task AddAsync(CashierSession session, CancellationToken ct = default);
    Task<List<Order>> GetOrdersAsync(Guid sessionId, CancellationToken ct = default);
    Task<List<string>> GetDistinctCashiersAsync(CancellationToken ct = default);
    Task<CashierSession?> GetOpenSessionByCashierAsync(
    string cashierName, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);


}
