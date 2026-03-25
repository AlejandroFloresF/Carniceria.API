using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public enum DebtStatus { Pending, Paid }

public class CustomerDebt : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public string OrderFolio { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DebtStatus Status { get; private set; } = DebtStatus.Pending;
    public DateTime? PaidAt { get; private set; }
    public string? Note { get; private set; }


    private CustomerDebt() { }

    public static CustomerDebt Create(
    Guid customerId, string customerName,
    Guid orderId, string orderFolio,
    decimal amount, string? note = null)
    {
        if (amount <= 0) throw new DomainException("Debt amount must be positive.");
        return new CustomerDebt
        {
            CustomerId = customerId,
            CustomerName = customerName,
            OrderId = orderId,
            OrderFolio = orderFolio,
            Amount = amount,
            Note = note,
        };
    }

    public PaymentMethod? PaidWithMethod { get; private set; }
    public decimal PaidCashReceived { get; private set; }

    public void MarkAsPaid(PaymentMethod method, decimal cashReceived)
    {
        if (Status == DebtStatus.Paid)
            throw new DomainException("Debt is already paid.");
        Status = DebtStatus.Paid;
        PaidAt = DateTime.UtcNow;
        PaidWithMethod = method;
        PaidCashReceived = cashReceived;
        SetUpdated();
    }
}