using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class CustomerOrderItem : BaseEntity
{
    public Guid CustomerOrderId { get; private set; }
    public Guid ProductId       { get; private set; }
    public string ProductName   { get; private set; } = string.Empty;
    public decimal QuantityKg   { get; private set; }

    // Navigation property for EF queries
    public CustomerOrder CustomerOrder { get; private set; } = null!;

    private CustomerOrderItem() { }

    public static CustomerOrderItem Create(
        Guid customerOrderId, Guid productId, string productName, decimal quantityKg)
    {
        return new CustomerOrderItem
        {
            CustomerOrderId = customerOrderId,
            ProductId       = productId,
            ProductName     = productName,
            QuantityKg      = quantityKg,
        };
    }
}
