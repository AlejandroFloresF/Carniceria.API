using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface ICashWithdrawalRepository
{
    Task AddAsync(CashWithdrawal withdrawal, CancellationToken ct);
    Task<List<CashWithdrawal>> GetBySessionAsync(Guid sessionId, CancellationToken ct);
}
