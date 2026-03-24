using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class StockAlert : BaseEntity
{
    public Guid ProductId { get; private set; }
    public decimal MinimumStockKg { get; private set; }
    public bool IsActive { get; private set; } = true;

    private StockAlert() { }

    public static StockAlert Create(Guid productId, decimal minimumKg)
    {
        if (minimumKg <= 0)
            throw new DomainException("Minimum stock must be positive.");

        return new StockAlert { ProductId = productId, MinimumStockKg = minimumKg };
    }

    public void Update(decimal minimumKg)
    {
        if (minimumKg <= 0)
            throw new DomainException("Minimum stock must be positive.");
        MinimumStockKg = minimumKg;
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
}