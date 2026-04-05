using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class CustomerOrder : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    // None, Weekly, Biweekly, Monthly, Bimonthly, Annual
    public string Recurrence { get; private set; } = "None";
    public DateTime NextDeliveryDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<CustomerOrderItem> _items = new();
    public IReadOnlyCollection<CustomerOrderItem> Items => _items.AsReadOnly();

    private CustomerOrder() { }

    public static CustomerOrder Create(
        Guid customerId, string customerName,
        string recurrence, DateTime nextDeliveryDate, string? notes)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");

        return new CustomerOrder
        {
            CustomerId       = customerId,
            CustomerName     = customerName,
            Recurrence       = recurrence,
            NextDeliveryDate = nextDeliveryDate,
            Notes            = notes,
        };
    }

    public void AddItem(Guid productId, string productName, decimal quantityKg)
    {
        if (quantityKg <= 0) throw new DomainException("Quantity must be positive.");
        if (_items.Any(i => i.ProductId == productId))
            throw new DomainException($"Product {productName} is already in this order.");
        _items.Add(CustomerOrderItem.Create(Id, productId, productName, quantityKg));
    }

    public void Update(string recurrence, DateTime nextDeliveryDate, string? notes)
    {
        Recurrence       = recurrence;
        NextDeliveryDate = nextDeliveryDate;
        Notes            = notes;
        SetUpdated();
    }

    public void ReplaceItems(List<(Guid ProductId, string ProductName, decimal QuantityKg)> items)
    {
        _items.Clear();
        foreach (var item in items)
            _items.Add(CustomerOrderItem.Create(Id, item.ProductId, item.ProductName, item.QuantityKg));
        SetUpdated();
    }

    public void AdvanceDeliveryDate()
    {
        if (Recurrence == "None") return;
        NextDeliveryDate = Recurrence switch
        {
            "Weekly"    => NextDeliveryDate.AddDays(7),
            "Biweekly"  => NextDeliveryDate.AddDays(14),
            "Monthly"   => NextDeliveryDate.AddMonths(1),
            "Bimonthly" => NextDeliveryDate.AddMonths(2),
            "Annual"    => NextDeliveryDate.AddYears(1),
            _           => NextDeliveryDate,
        };
        SetUpdated();
    }

    public void Deactivate() { IsActive = false; SetUpdated(); }
}
