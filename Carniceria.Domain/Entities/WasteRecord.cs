using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public enum WasteReason { Expired, ProcessLoss, Damaged, Other }

public class WasteRecord : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal QuantityKg { get; private set; }
    public WasteReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime WasteDate { get; private set; }

    private WasteRecord() { }

    public static WasteRecord Create(
        Guid productId, string productName,
        decimal quantityKg, WasteReason reason, string? notes = null)
    {
        if (quantityKg <= 0)
            throw new DomainException("Quantity must be positive.");

        return new WasteRecord
        {
            ProductId = productId,
            ProductName = productName,
            QuantityKg = quantityKg,
            Reason = reason,
            Notes = notes,
            WasteDate = DateTime.UtcNow,
        };
    }
}