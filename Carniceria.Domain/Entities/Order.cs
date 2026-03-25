using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public enum OrderStatus { Pending, Completed, Cancelled }
public enum PaymentMethod { Cash, Card, Transfer, PayLater }

public class Order : BaseEntity
{
    public Guid CashierSessionId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal CashReceived { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Subtotal => _items.Sum(i => i.UnitPrice * i.Quantity);
    public decimal DiscountAmount => Math.Round(Subtotal * (DiscountPercent / 100), 2);
    public decimal TaxAmount => Math.Round((Subtotal - DiscountAmount) * 0.16m, 2);
    public decimal Total => Subtotal - DiscountAmount + TaxAmount;

    private Order() { }

    public static Order Create(Guid cashierSessionId) =>
        new() { CashierSessionId = cashierSessionId };

    public void AddItem(Product product, decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
            existing.AddQuantity(quantity);
        else
            _items.Add(OrderItem.Create(Id, product.Id, product.Name, product.PricePerUnit, quantity));
    }

    // ← nuevo: usa precio especial del cliente en lugar del precio general
    public void AddItemWithCustomPrice(Product product, decimal quantity, decimal customPrice)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
            existing.AddQuantity(quantity);
        else
            _items.Add(OrderItem.Create(Id, product.Id, product.Name, customPrice, quantity));
    }

    public void ApplyDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new DomainException("Discount must be between 0 and 100.");
        DiscountPercent = percent;
    }

    public void AssignCustomer(Customer customer)
    {
        CustomerId = customer.Id;
        CustomerName = customer.Name;
    }

    public void Confirm(PaymentMethod method, decimal cashReceived)
    {
        if (!_items.Any()) throw new DomainException("Cannot confirm an empty order.");
        if (method == PaymentMethod.Cash && cashReceived < Total)
            throw new DomainException("Cash received is less than total.");
        PaymentMethod = method;
        CashReceived = cashReceived;
        Status = OrderStatus.Completed;
        SetUpdated();
    }

    public void ConfirmPayLater(Guid customerId, string customerName, decimal advancePayment = 0)
    {
        if (!_items.Any()) throw new DomainException("Cannot confirm an empty order.");
        if (customerId == Guid.Empty)
            throw new DomainException("PayLater requires a registered customer.");
        if (advancePayment < 0 || advancePayment >= Total)
            throw new DomainException("Advance payment must be between 0 and the total.");
        CustomerId = customerId;
        CustomerName = customerName;
        PaymentMethod = PaymentMethod.PayLater;
        CashReceived = advancePayment;
        Status = OrderStatus.Completed;
        SetUpdated();
    }
}