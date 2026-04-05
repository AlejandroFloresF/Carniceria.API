using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface ICustomerOrderRepository
{
    Task<List<CustomerOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CustomerOrder>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns total kg reserved per product across ALL active orders.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetTotalReservedByProductAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns total kg reserved per product for a specific customer's active orders.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetReservedByProductForCustomerAsync(Guid customerId, CancellationToken ct = default);

    Task AddAsync(CustomerOrder order, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes all items for the given order directly in the DB,
    /// then saves the updated order (with new items) in a second pass.
    /// Required because EF Core won't cascade-delete orphans from a private backing list.
    /// </summary>
    Task UpdateWithItemReplacementAsync(CustomerOrder order, CancellationToken ct = default);
}
