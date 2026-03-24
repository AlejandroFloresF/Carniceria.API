using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;

namespace Carniceria.Domain.Interfaces;

public interface IInventoryRepository
{
    Task AddEntryAsync(InventoryEntry entry, CancellationToken ct = default);
    Task<List<InventoryEntry>> GetEntriesAsync(Guid? productId, DateTime? from, DateTime? to, CancellationToken ct = default);

    Task AddWasteAsync(WasteRecord waste, CancellationToken ct = default);
    Task<List<WasteRecord>> GetWasteAsync(Guid? productId, DateTime? from, DateTime? to, CancellationToken ct = default);

    Task<List<StockAlert>> GetAlertsAsync(CancellationToken ct = default);
    Task<StockAlert?> GetAlertByProductAsync(Guid productId, CancellationToken ct = default);
    Task AddAlertAsync(StockAlert alert, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // ← ahora usa StockMovement del Domain
    Task<List<StockMovement>> GetMovementsAsync(Guid productId, DateTime? from, DateTime? to, CancellationToken ct = default);
}