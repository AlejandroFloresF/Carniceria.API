using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
}
