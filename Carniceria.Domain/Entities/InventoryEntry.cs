using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public enum EntrySource { Ranch, Supplier, Adjustment }

public class InventoryEntry : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal QuantityKg { get; private set; }
    public decimal CostPerKg { get; private set; }
    public EntrySource Source { get; private set; }
    public string? Notes { get; private set; }
    public DateTime EntryDate { get; private set; }

    private InventoryEntry() { }

    public static InventoryEntry Create(
        Guid productId, string productName,
        decimal quantityKg, decimal costPerKg,
        EntrySource source, string? notes = null)
    {
        if (quantityKg <= 0)
            throw new DomainException("Quantity must be positive.");
        if (costPerKg < 0)
            throw new DomainException("Cost cannot be negative.");

        return new InventoryEntry
        {
            ProductId = productId,
            ProductName = productName,
            QuantityKg = quantityKg,
            CostPerKg = costPerKg,
            Source = source,
            Notes = notes,
            EntryDate = DateTime.UtcNow,
        };
    }
}