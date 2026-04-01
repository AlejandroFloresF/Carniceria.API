using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;

public class ScheduledExpense : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = "General";
    public string Recurrence { get; private set; } = "None"; // None, Weekly, Biweekly, Monthly, Annual
    public DateTime NextDueDate { get; private set; }
    public int AlertDaysBefore { get; private set; } = 3;
    public bool IsActive { get; private set; } = true;

    private ScheduledExpense() { }

    public static ScheduledExpense Create(string name, decimal amount, string category,
        string recurrence, DateTime nextDueDate, int alertDaysBefore = 3, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es requerido.");
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");
        return new ScheduledExpense
        {
            Name = name, Description = description, Amount = amount, Category = category,
            Recurrence = recurrence, NextDueDate = nextDueDate, AlertDaysBefore = alertDaysBefore
        };
    }

    public void Update(string name, decimal amount, string category, string recurrence,
        DateTime nextDueDate, int alertDaysBefore, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es requerido.");
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");
        Name = name; Amount = amount; Category = category; Recurrence = recurrence;
        NextDueDate = nextDueDate; AlertDaysBefore = alertDaysBefore; Description = description;
        SetUpdated();
    }

    public void AdvanceDueDate()
    {
        NextDueDate = Recurrence switch
        {
            "Weekly"   => NextDueDate.AddDays(7),
            "Biweekly" => NextDueDate.AddDays(14),
            "Monthly"  => NextDueDate.AddMonths(1),
            "Annual"   => NextDueDate.AddYears(1),
            _          => NextDueDate
        };
        SetUpdated();
    }

    public void ToggleActive() { IsActive = !IsActive; SetUpdated(); }
}
