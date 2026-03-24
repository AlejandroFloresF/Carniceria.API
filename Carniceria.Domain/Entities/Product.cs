using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal PricePerUnit { get; private set; }
    public string Unit { get; private set; } = "kg";
    public decimal StockKg { get; private set; }
    public bool IsActive { get; private set; } = true;
    private Product() { }
    public static Product Create(string name, string category, decimal price, string unit, decimal stock)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (price <= 0) throw new DomainException("Price must be greater than zero.");
        if (stock < 0) throw new DomainException("Stock cannot be negative.");
        return new Product { Name = name, Category = category, PricePerUnit = price, Unit = unit, StockKg = stock };
    }
    public void DeductStock(decimal qty)
    {
        if (qty <= 0) throw new DomainException("Quantity must be positive.");
        if (StockKg < qty) throw new DomainException($"Insufficient stock for {Name}.");
        StockKg -= qty;
        SetUpdated();
    }
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new DomainException("Price must be greater than zero.");
        PricePerUnit = newPrice;
        SetUpdated();
    }
    public void Deactivate() { IsActive = false; SetUpdated(); }

    public void AddStock(decimal qty)
    {
        if (qty <= 0) throw new DomainException("Quantity must be positive.");
        StockKg += qty;
        SetUpdated();
    }
}
