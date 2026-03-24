using Carniceria.Application.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Carniceria.Domain.Common;

namespace Carniceria.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;
    public InventoryRepository(AppDbContext db) => _db = db;

    // ── Entries ──────────────────────────────────────────────
    public async Task AddEntryAsync(InventoryEntry entry, CancellationToken ct = default)
        => await _db.InventoryEntries.AddAsync(entry, ct);

    public Task<List<InventoryEntry>> GetEntriesAsync(
        Guid? productId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = _db.InventoryEntries.AsQueryable();
        if (productId.HasValue) q = q.Where(e => e.ProductId == productId);
        if (from.HasValue) q = q.Where(e => e.EntryDate >= from);
        if (to.HasValue) q = q.Where(e => e.EntryDate <= to);
        return q.OrderByDescending(e => e.EntryDate).ToListAsync(ct);
    }

    // ── Waste ────────────────────────────────────────────────
    public async Task AddWasteAsync(WasteRecord waste, CancellationToken ct = default)
        => await _db.WasteRecords.AddAsync(waste, ct);

    public Task<List<WasteRecord>> GetWasteAsync(
        Guid? productId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = _db.WasteRecords.AsQueryable();
        if (productId.HasValue) q = q.Where(w => w.ProductId == productId);
        if (from.HasValue) q = q.Where(w => w.WasteDate >= from);
        if (to.HasValue) q = q.Where(w => w.WasteDate <= to);
        return q.OrderByDescending(w => w.WasteDate).ToListAsync(ct);
    }

    // ── Alerts ───────────────────────────────────────────────
    public Task<List<StockAlert>> GetAlertsAsync(CancellationToken ct = default)
        => _db.StockAlerts.Where(a => a.IsActive).ToListAsync(ct);

    public Task<StockAlert?> GetAlertByProductAsync(Guid productId, CancellationToken ct = default)
        => _db.StockAlerts.FirstOrDefaultAsync(
               a => a.ProductId == productId && a.IsActive, ct);

    public async Task AddAlertAsync(StockAlert alert, CancellationToken ct = default)
        => await _db.StockAlerts.AddAsync(alert, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    // ── Movements ────────────────────────────────────────────
    public async Task<List<StockMovement>> GetMovementsAsync(
    Guid productId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var entries = await _db.InventoryEntries
            .Where(e => e.ProductId == productId
                     && e.EntryDate >= fromDate
                     && e.EntryDate <= toDate)
            .ToListAsync(ct);

        var wastes = await _db.WasteRecords
            .Where(w => w.ProductId == productId
                     && w.WasteDate >= fromDate
                     && w.WasteDate <= toDate)
            .ToListAsync(ct);

        var sales = await (
            from item in _db.OrderItems
            join order in _db.Orders on item.OrderId equals order.Id
            where item.ProductId == productId
               && order.Status == OrderStatus.Completed
               && order.CreatedAt >= fromDate
               && order.CreatedAt <= toDate
            select new { item.Quantity, order.CreatedAt }
        ).ToListAsync(ct);

        var movements = new List<StockMovement>();

        movements.AddRange(entries.Select(e => new StockMovement(
            e.EntryDate, $"Entrada ({e.Source})", e.QuantityKg, 0, e.Notes)));

        movements.AddRange(wastes.Select(w => new StockMovement(
            w.WasteDate, $"Merma ({w.Reason})", -w.QuantityKg, 0, w.Notes)));

        movements.AddRange(sales.Select(s => new StockMovement(
            s.CreatedAt, "Venta", -s.Quantity, 0, null)));

        return movements.OrderByDescending(m => m.Date).ToList();
    }
}