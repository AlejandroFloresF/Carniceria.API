using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Expenses;

public record GetScheduledExpensesQuery : IRequest<Result<List<ScheduledExpenseDto>>>;

public class GetScheduledExpensesHandler : IRequestHandler<GetScheduledExpensesQuery, Result<List<ScheduledExpenseDto>>>
{
    private readonly IExpenseRepository _repo;
    public GetScheduledExpensesHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<List<ScheduledExpenseDto>>> Handle(GetScheduledExpensesQuery _, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expenses = await _repo.GetScheduledActiveAsync(ct);
        var dtos = expenses.Select(e => new ScheduledExpenseDto(
            e.Id, e.Name, e.Description, e.Amount, e.Category, e.Recurrence,
            e.NextDueDate, e.AlertDaysBefore, e.IsActive,
            IsOverdue: e.NextDueDate < now,
            IsUpcoming: e.NextDueDate >= now && e.NextDueDate <= now.AddDays(e.AlertDaysBefore)
        )).OrderBy(e => e.NextDueDate).ToList();
        return Result.Ok(dtos);
    }
}

public record GetExpenseRequestsQuery(string? Status, string? RequestedBy, DateTime? From = null, DateTime? To = null) : IRequest<Result<List<ExpenseRequestDto>>>;

public class GetExpenseRequestsHandler : IRequestHandler<GetExpenseRequestsQuery, Result<List<ExpenseRequestDto>>>
{
    private readonly IExpenseRepository _repo;
    public GetExpenseRequestsHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<List<ExpenseRequestDto>>> Handle(GetExpenseRequestsQuery q, CancellationToken ct)
    {
        var requests = await _repo.GetRequestsAsync(q.Status, q.RequestedBy, ct);
        var filtered = requests.AsEnumerable();
        if (q.From.HasValue) filtered = filtered.Where(r => r.RequestedAt >= q.From.Value);
        if (q.To.HasValue)   filtered = filtered.Where(r => r.RequestedAt <= q.To.Value);
        var dtos = filtered.Select(r => new ExpenseRequestDto(
            r.Id, r.Description, r.Amount, r.Category, r.Status,
            r.RequestedBy, r.SessionId, r.ScheduledExpenseId,
            r.ReviewedBy, r.DenyReason, r.Notes, r.RequestedAt, r.ReviewedAt
        )).OrderByDescending(r => r.RequestedAt).ToList();
        return Result.Ok(dtos);
    }
}

public record GetExpenseNotificationsQuery(string? CashierName) : IRequest<Result<ExpenseNotificationsDto>>;

public class GetExpenseNotificationsHandler : IRequestHandler<GetExpenseNotificationsQuery, Result<ExpenseNotificationsDto>>
{
    private readonly IExpenseRepository _repo;
    public GetExpenseNotificationsHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<ExpenseNotificationsDto>> Handle(GetExpenseNotificationsQuery q, CancellationToken ct)
    {
        var now       = DateTime.UtcNow;
        var scheduled = await _repo.GetScheduledActiveAsync(ct);
        var items     = new List<ExpenseNotificationItemDto>();

        // Upcoming / overdue scheduled expenses — shown to everyone
        var alertExpenses = scheduled
            .Where(e => e.NextDueDate <= now.AddDays(e.AlertDaysBefore))
            .OrderBy(e => e.NextDueDate)
            .ToList();

        foreach (var e in alertExpenses)
        {
            var isOverdue = e.NextDueDate < now;
            var daysLeft  = (int)(e.NextDueDate - now).TotalDays;
            items.Add(new ExpenseNotificationItemDto(
                Type: "UpcomingExpense",
                Title: e.Name,
                Subtitle: isOverdue
                    ? $"Vencido hace {Math.Abs(daysLeft)} día(s) · ${e.Amount:F2}"
                    : daysLeft == 0
                        ? $"Vence hoy · ${e.Amount:F2}"
                        : $"Vence en {daysLeft} día(s) · ${e.Amount:F2}",
                ReferenceId: e.Id,
                Severity: isOverdue ? "danger" : daysLeft <= 1 ? "warning" : "info"
            ));
        }

        int pendingCount;

        if (string.IsNullOrWhiteSpace(q.CashierName))
        {
            // ── Admin view: show all pending requests ──────────────────────────
            var pending = await _repo.GetRequestsAsync("Pending", null, ct);
            pendingCount = pending.Count;
            foreach (var r in pending)
            {
                items.Add(new ExpenseNotificationItemDto(
                    Type: "PendingRequest",
                    Title: r.Description,
                    Subtitle: $"{r.RequestedBy} · ${r.Amount:F2}",
                    ReferenceId: r.Id,
                    Severity: "warning"
                ));
            }
        }
        else
        {
            // ── Cashier view: show recently reviewed requests for this cashier ─
            pendingCount = 0;
            var since    = now.AddHours(-24);
            var reviewed = await _repo.GetRequestsAsync(null, q.CashierName, ct);
            var recent   = reviewed
                .Where(r => r.Status != "Pending" && r.ReviewedAt.HasValue && r.ReviewedAt.Value >= since)
                .OrderByDescending(r => r.ReviewedAt)
                .ToList();

            foreach (var r in recent)
            {
                var approved = r.Status == "Approved";
                items.Add(new ExpenseNotificationItemDto(
                    Type: "RequestReviewed",
                    Title: approved ? "Solicitud aprobada" : "Solicitud denegada",
                    Subtitle: $"{r.Description} · ${r.Amount:F2}"
                        + (r.DenyReason is not null ? $" · {r.DenyReason}" : ""),
                    ReferenceId: r.Id,
                    Severity: approved ? "success" : "danger"
                ));
            }
        }

        return Result.Ok(new ExpenseNotificationsDto(
            PendingRequestsCount: pendingCount,
            UpcomingExpensesCount: alertExpenses.Count,
            Items: items
        ));
    }
}
