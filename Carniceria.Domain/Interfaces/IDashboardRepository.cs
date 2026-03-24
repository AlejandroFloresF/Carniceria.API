using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<List<Order>> GetOrdersInRangeAsync(
        DateTime from, DateTime to, Guid? sessionId, CancellationToken ct = default);
}