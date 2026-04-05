using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Sessions.Queries;
public record GetSessionSummaryQuery(Guid SessionId) : IRequest<Result<SessionSummaryDto>>;
public class GetSessionSummaryHandler : IRequestHandler<GetSessionSummaryQuery, Result<SessionSummaryDto>>
{
    private readonly ISessionRepository _sessions;
    private readonly ICustomerDebtRepository _debts;
    private readonly IExpenseRepository _expenses;
    public GetSessionSummaryHandler(ISessionRepository sessions, ICustomerDebtRepository debts, IExpenseRepository expenses)
    {
        _sessions = sessions;
        _debts    = debts;
        _expenses = expenses;
    }
    public async Task<Result<SessionSummaryDto>> Handle(GetSessionSummaryQuery q, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(q.SessionId, ct);
        if (session is null) return Result.Fail<SessionSummaryDto>("Session not found.");
        var orders = await _sessions.GetOrdersAsync(q.SessionId, ct);
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        // Cobros de deuda durante el turno
        var sessionTo  = session.ClosedAt ?? DateTime.UtcNow;
        var paidDebts  = await _debts.GetPaidInRangeAsync(session.OpenedAt, sessionTo, ct);
        var debtCash   = paidDebts.Where(d => d.PaidWithMethod == PaymentMethod.Cash).Sum(d => d.Amount);
        var debtCard   = paidDebts.Where(d => d.PaidWithMethod == PaymentMethod.Card).Sum(d => d.Amount);
        var debtTransf = paidDebts.Where(d => d.PaidWithMethod == PaymentMethod.Transfer).Sum(d => d.Amount);
        var totalDebtPayments = paidDebts.Sum(d => d.Amount);

        // Anticipos por método
        var advCash  = completed.Where(o => o.PaymentMethod == PaymentMethod.PayLater
                                         && (o.AdvancePaymentMethod == PaymentMethod.Cash || o.AdvancePaymentMethod == null))
                                .Sum(o => o.CashReceived);
        var advCard  = completed.Where(o => o.PaymentMethod == PaymentMethod.PayLater
                                         && o.AdvancePaymentMethod == PaymentMethod.Card).Sum(o => o.CashReceived);
        var advTransf = completed.Where(o => o.PaymentMethod == PaymentMethod.PayLater
                                          && o.AdvancePaymentMethod == PaymentMethod.Transfer).Sum(o => o.CashReceived);

        var totalCash     = completed.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total) + advCash + debtCash;
        var totalCard     = completed.Where(o => o.PaymentMethod == PaymentMethod.Card).Sum(o => o.Total) + advCard + debtCard;
        var totalTransfer = completed.Where(o => o.PaymentMethod == PaymentMethod.Transfer).Sum(o => o.Total) + advTransf + debtTransf;

        var totalExpenses = await _expenses.GetApprovedTotalBySessionAsync(q.SessionId, ct);

        return Result.Ok(new SessionSummaryDto(
            session.Id, session.CashierName, session.OpenedAt, session.ClosedAt,
            completed.Count, completed.Sum(o => o.Total),
            totalCash, totalCard, totalTransfer,
            completed.Sum(o => o.DiscountAmount), session.OpeningCash,
            session.CurrentCash,
            totalDebtPayments,
            totalExpenses));
    }
}
