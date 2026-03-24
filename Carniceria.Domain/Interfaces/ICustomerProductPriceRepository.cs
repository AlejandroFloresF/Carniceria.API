using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface ICustomerProductPriceRepository
{
    Task<List<CustomerProductPrice>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerProductPrice?> GetAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    Task UpsertAsync(CustomerProductPrice price, CancellationToken ct = default);
    Task DeleteAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}