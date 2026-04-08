using Carniceria.Domain.Common;

namespace Carniceria.Domain.Entities;

public class CashWithdrawal : BaseEntity
{
    public Guid   SessionId   { get; private set; }
    public string CashierName { get; private set; } = string.Empty;
    public decimal Amount     { get; private set; }
    public string? Note       { get; private set; }

    private CashWithdrawal() { }

    public static CashWithdrawal Create(Guid sessionId, string cashierName, decimal amount, string? note)
    {
        if (amount <= 0) throw new DomainException("El monto del retiro debe ser mayor a cero.");
        return new CashWithdrawal
        {
            SessionId   = sessionId,
            CashierName = cashierName,
            Amount      = amount,
            Note        = note,
        };
    }
}
