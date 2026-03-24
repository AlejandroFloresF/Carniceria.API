using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public enum SessionStatus { Open, Closed }
public class CashierSession : BaseEntity
{
    public string CashierName { get; private set; } = string.Empty;
    public DateTime OpenedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; private set; }
    public decimal OpeningCash { get; private set; }
    public decimal ClosingCash { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Open;
    private CashierSession() { }
    public static CashierSession Open(string cashierName, decimal openingCash)
    {
        if (string.IsNullOrWhiteSpace(cashierName)) throw new DomainException("Cashier name is required.");
        if (openingCash < 0) throw new DomainException("Opening cash cannot be negative.");
        return new CashierSession { CashierName = cashierName, OpeningCash = openingCash };
    }
    public void Close(decimal closingCash)
    {
        if (Status == SessionStatus.Closed) throw new DomainException("Session is already closed.");
        ClosingCash = closingCash;
        ClosedAt = DateTime.UtcNow;
        Status = SessionStatus.Closed;
        SetUpdated();
    }
}
