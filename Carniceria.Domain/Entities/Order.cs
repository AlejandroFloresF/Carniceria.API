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
    public decimal Total => Math.Round(Subtotal - DiscountAmount, 2);

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
        // Tolerancia de 1 centavo para absorber diferencias de redondeo float→decimal
        if (method == PaymentMethod.Cash && cashReceived < Total - 0.01m)
            throw new DomainException($"Monto recibido (${cashReceived:F2}) es menor al total (${Total:F2}).");
        PaymentMethod = method;
        CashReceived = cashReceived;
        Status = OrderStatus.Completed;
        SetUpdated();
    }

    /// <summary>Set when this sale was generated from a registered customer order (pedido).</summary>
    public Guid? SourceCustomerOrderId { get; private set; }

    public void MarkAsFromCustomerOrder(Guid customerOrderId)
    {
        SourceCustomerOrderId = customerOrderId;
    }

    /// <summary>Second payment method when the customer pays with two methods (e.g. Cash + Transfer).</summary>
    public PaymentMethod? SecondaryPaymentMethod { get; private set; }
    public decimal SecondaryAmount { get; private set; }

    public void ConfirmSplit(decimal cashAmount, PaymentMethod secondaryMethod, decimal secondaryAmount)
    {
        if (!_items.Any()) throw new DomainException("Cannot confirm an empty order.");
        if (secondaryMethod == PaymentMethod.Cash || secondaryMethod == PaymentMethod.PayLater)
            throw new DomainException("El método secundario no puede ser Efectivo o A crédito.");
        if (cashAmount + secondaryAmount < Total - 0.01m)
            throw new DomainException($"El total de los pagos (${cashAmount + secondaryAmount:F2}) es menor al total de la orden (${Total:F2}).");
        PaymentMethod = PaymentMethod.Cash;
        CashReceived = cashAmount;
        SecondaryPaymentMethod = secondaryMethod;
        SecondaryAmount = secondaryAmount;
        Status = OrderStatus.Completed;
        SetUpdated();
    }

    public PaymentMethod? AdvancePaymentMethod { get; private set; }

    public void ConfirmPayLater(Guid customerId, string customerName, decimal advancePayment = 0, PaymentMethod? advancePaymentMethod = null)
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
        AdvancePaymentMethod = advancePayment > 0 ? advancePaymentMethod : null;
        Status = OrderStatus.Completed;
        SetUpdated();
    }
}