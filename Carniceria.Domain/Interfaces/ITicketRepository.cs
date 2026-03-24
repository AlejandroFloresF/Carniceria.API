using Carniceria.Domain.Entities;
namespace Carniceria.Domain.Interfaces;
public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> GetByFolioAsync(string folio, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task<int> GetNextFolioNumberAsync(CancellationToken ct = default);
    Task<Ticket?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

}
