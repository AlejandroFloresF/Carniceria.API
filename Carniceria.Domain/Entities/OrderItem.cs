using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = "kg";
    public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2);

    // ← navegación necesaria para el join en InventoryRepository
    public Order Order { get; private set; } = null!;

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, Guid productId, string name, decimal price, decimal qty) =>
        new()
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = name,
            UnitPrice = price,
            Quantity = qty,
        };

    public void AddQuantity(decimal qty) => Quantity += qty;
}