using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;

public interface IExpenseRepository
{
    Task<List<ScheduledExpense>> GetScheduledActiveAsync(CancellationToken ct);
    Task<ScheduledExpense?> GetScheduledByIdAsync(Guid id, CancellationToken ct);
    Task AddScheduledAsync(ScheduledExpense expense, CancellationToken ct);
    Task DeleteScheduledAsync(ScheduledExpense expense, CancellationToken ct);
    Task<List<ExpenseRequest>> GetRequestsAsync(string? status, string? requestedBy, CancellationToken ct);
    Task<decimal> GetApprovedTotalBySessionAsync(Guid sessionId, CancellationToken ct);
    Task<List<ExpenseRequest>> GetApprovedBySessionAsync(Guid sessionId, CancellationToken ct);
    Task<ExpenseRequest?> GetRequestByIdAsync(Guid id, CancellationToken ct);
    Task AddRequestAsync(ExpenseRequest request, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
