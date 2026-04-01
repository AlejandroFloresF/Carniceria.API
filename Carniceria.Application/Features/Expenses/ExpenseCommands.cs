using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Expenses;

// ── Scheduled Expenses ────────────────────────────────────────────────────────

public record CreateScheduledExpenseCommand(
    string Name, decimal Amount, string Category, string Recurrence,
    DateTime NextDueDate, int AlertDaysBefore, string? Description
) : IRequest<Result<Guid>>;

public class CreateScheduledExpenseHandler : IRequestHandler<CreateScheduledExpenseCommand, Result<Guid>>
{
    private readonly IExpenseRepository _repo;
    public CreateScheduledExpenseHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<Guid>> Handle(CreateScheduledExpenseCommand cmd, CancellationToken ct)
    {
        try
        {
            var expense = ScheduledExpense.Create(cmd.Name, cmd.Amount, cmd.Category,
                cmd.Recurrence, cmd.NextDueDate, cmd.AlertDaysBefore, cmd.Description);
            await _repo.AddScheduledAsync(expense, ct);
            return Result.Ok(expense.Id);
        }
        catch (DomainException ex) { return Result.Fail<Guid>(ex.Message); }
    }
}

public record UpdateScheduledExpenseCommand(
    Guid Id, string Name, decimal Amount, string Category, string Recurrence,
    DateTime NextDueDate, int AlertDaysBefore, string? Description
) : IRequest<Result<bool>>;

public class UpdateScheduledExpenseHandler : IRequestHandler<UpdateScheduledExpenseCommand, Result<bool>>
{
    private readonly IExpenseRepository _repo;
    public UpdateScheduledExpenseHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<bool>> Handle(UpdateScheduledExpenseCommand cmd, CancellationToken ct)
    {
        var expense = await _repo.GetScheduledByIdAsync(cmd.Id, ct);
        if (expense is null) return Result.Fail<bool>("Gasto no encontrado.");
        try
        {
            expense.Update(cmd.Name, cmd.Amount, cmd.Category, cmd.Recurrence,
                cmd.NextDueDate, cmd.AlertDaysBefore, cmd.Description);
            await _repo.SaveChangesAsync(ct);
            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}

public record DeleteScheduledExpenseCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteScheduledExpenseHandler : IRequestHandler<DeleteScheduledExpenseCommand, Result<bool>>
{
    private readonly IExpenseRepository _repo;
    public DeleteScheduledExpenseHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<bool>> Handle(DeleteScheduledExpenseCommand cmd, CancellationToken ct)
    {
        var expense = await _repo.GetScheduledByIdAsync(cmd.Id, ct);
        if (expense is null) return Result.Fail<bool>("Gasto no encontrado.");
        await _repo.DeleteScheduledAsync(expense, ct);
        return Result.Ok(true);
    }
}

public record ToggleScheduledExpenseCommand(Guid Id) : IRequest<Result<bool>>;

public class ToggleScheduledExpenseHandler : IRequestHandler<ToggleScheduledExpenseCommand, Result<bool>>
{
    private readonly IExpenseRepository _repo;
    public ToggleScheduledExpenseHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<bool>> Handle(ToggleScheduledExpenseCommand cmd, CancellationToken ct)
    {
        var expense = await _repo.GetScheduledByIdAsync(cmd.Id, ct);
        if (expense is null) return Result.Fail<bool>("Gasto no encontrado.");
        expense.ToggleActive();
        await _repo.SaveChangesAsync(ct);
        return Result.Ok(true);
    }
}

// ── Expense Requests ──────────────────────────────────────────────────────────

public record CreateExpenseRequestCommand(
    string Description, decimal Amount, string Category,
    string RequestedBy, Guid? SessionId, Guid? ScheduledExpenseId, string? Notes
) : IRequest<Result<Guid>>;

public class CreateExpenseRequestHandler : IRequestHandler<CreateExpenseRequestCommand, Result<Guid>>
{
    private readonly IExpenseRepository _repo;
    public CreateExpenseRequestHandler(IExpenseRepository repo) => _repo = repo;
    public async Task<Result<Guid>> Handle(CreateExpenseRequestCommand cmd, CancellationToken ct)
    {
        try
        {
            var request = ExpenseRequest.Create(cmd.Description, cmd.Amount, cmd.Category,
                cmd.RequestedBy, cmd.SessionId, cmd.ScheduledExpenseId, cmd.Notes);
            await _repo.AddRequestAsync(request, ct);
            return Result.Ok(request.Id);
        }
        catch (DomainException ex) { return Result.Fail<Guid>(ex.Message); }
    }
}

public record ReviewExpenseRequestCommand(
    Guid RequestId, bool Approved, string ReviewedBy, string? DenyReason
) : IRequest<Result<bool>>;

public class ReviewExpenseRequestHandler : IRequestHandler<ReviewExpenseRequestCommand, Result<bool>>
{
    private readonly IExpenseRepository _expenseRepo;
    private readonly ISessionRepository _sessionRepo;
    public ReviewExpenseRequestHandler(IExpenseRepository expenseRepo, ISessionRepository sessionRepo)
    {
        _expenseRepo = expenseRepo;
        _sessionRepo = sessionRepo;
    }

    public async Task<Result<bool>> Handle(ReviewExpenseRequestCommand cmd, CancellationToken ct)
    {
        var request = await _expenseRepo.GetRequestByIdAsync(cmd.RequestId, ct);
        if (request is null) return Result.Fail<bool>("Solicitud no encontrada.");
        try
        {
            if (cmd.Approved)
            {
                request.Approve(cmd.ReviewedBy);

                // Deduct cash from session if one is linked
                if (request.SessionId.HasValue)
                {
                    var session = await _sessionRepo.GetByIdAsync(request.SessionId.Value, ct);
                    if (session is not null && session.Status == SessionStatus.Open)
                        session.DeductCash(request.Amount);
                }

                // Advance due date on scheduled expense if linked
                if (request.ScheduledExpenseId.HasValue)
                {
                    var scheduled = await _expenseRepo.GetScheduledByIdAsync(request.ScheduledExpenseId.Value, ct);
                    if (scheduled is not null)
                    {
                        scheduled.AdvanceDueDate();
                        // One-time expenses are done after payment — deactivate them
                        if (scheduled.Recurrence == "None" && scheduled.IsActive)
                            scheduled.ToggleActive();
                    }
                }
            }
            else
            {
                request.Deny(cmd.ReviewedBy, cmd.DenyReason);
            }

            await _expenseRepo.SaveChangesAsync(ct);
            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}
