using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class CustomerProductPrice : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal CustomPrice { get; private set; }

    private CustomerProductPrice() { }

    public static CustomerProductPrice Create(Guid customerId, Guid productId, decimal price)
    {
        if (price <= 0) throw new DomainException("Price must be greater than zero.");
        return new CustomerProductPrice
        {
            CustomerId = customerId,
            ProductId = productId,
            CustomPrice = price,
        };
    }

    public void UpdatePrice(decimal price)
    {
        if (price <= 0) throw new DomainException("Price must be greater than zero.");
        CustomPrice = price;
        SetUpdated();
    }
}