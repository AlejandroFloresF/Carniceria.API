using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;

public class ExpenseRequest : BaseEntity
{
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = "General";
    public string Status { get; private set; } = "Pending"; // Pending, Approved, Denied
    public string RequestedBy { get; private set; } = string.Empty;
    public Guid? SessionId { get; private set; }
    public Guid? ScheduledExpenseId { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? DenyReason { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private ExpenseRequest() { }

    public static ExpenseRequest Create(string description, decimal amount, string category,
        string requestedBy, Guid? sessionId = null, Guid? scheduledExpenseId = null, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("La descripción es requerida.");
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");
        return new ExpenseRequest
        {
            Description = description, Amount = amount, Category = category,
            RequestedBy = requestedBy, SessionId = sessionId,
            ScheduledExpenseId = scheduledExpenseId, Notes = notes,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void Approve(string reviewedBy)
    {
        if (Status != "Pending") throw new DomainException("Solo se pueden aprobar solicitudes pendientes.");
        Status = "Approved"; ReviewedBy = reviewedBy; ReviewedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Deny(string reviewedBy, string? reason = null)
    {
        if (Status != "Pending") throw new DomainException("Solo se pueden denegar solicitudes pendientes.");
        Status = "Denied"; ReviewedBy = reviewedBy; DenyReason = reason; ReviewedAt = DateTime.UtcNow;
        SetUpdated();
    }
}
