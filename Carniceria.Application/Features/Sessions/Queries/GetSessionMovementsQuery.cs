using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Sessions.Queries;

public record GetSessionMovementsQuery(Guid SessionId) : IRequest<Result<List<CashMovementDto>>>;

public class GetSessionMovementsHandler : IRequestHandler<GetSessionMovementsQuery, Result<List<CashMovementDto>>>
{
    private readonly ISessionRepository        _sessions;
    private readonly ICustomerDebtRepository   _debts;
    private readonly IExpenseRepository        _expenses;
    private readonly ICashWithdrawalRepository _withdrawals;

    public GetSessionMovementsHandler(
        ISessionRepository sessions,
        ICustomerDebtRepository debts,
        IExpenseRepository expenses,
        ICashWithdrawalRepository withdrawals)
    {
        _sessions    = sessions;
        _debts       = debts;
        _expenses    = expenses;
        _withdrawals = withdrawals;
    }

    public async Task<Result<List<CashMovementDto>>> Handle(GetSessionMovementsQuery q, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(q.SessionId, ct);
        if (session is null) return Result.Fail<List<CashMovementDto>>("Session not found.");

        var movements = new List<CashMovementDto>();

        // ── 1. Apertura de turno ─────────────────────────────────────────────
        movements.Add(new CashMovementDto(
            session.OpenedAt,
            "Apertura",
            "Fondo inicial de caja",
            session.OpeningCash));

        // ── 2. Ventas en efectivo ────────────────────────────────────────────
        var orders = await _sessions.GetOrdersAsync(q.SessionId, ct);
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        foreach (var o in completed)
        {
            var customer = string.IsNullOrEmpty(o.CustomerName) ? "Público general" : o.CustomerName;

            if (o.SecondaryPaymentMethod.HasValue)
            {
                if (o.CashReceived > 0)
                    movements.Add(new CashMovementDto(o.CreatedAt, "Venta mixta", customer, o.CashReceived, o.Id));
            }
            else if (o.PaymentMethod == PaymentMethod.Cash)
            {
                movements.Add(new CashMovementDto(o.CreatedAt, "Venta", customer, o.Total, o.Id));
            }
            else if (o.PaymentMethod == PaymentMethod.PayLater && o.CashReceived > 0
                     && (o.AdvancePaymentMethod == PaymentMethod.Cash || o.AdvancePaymentMethod == null))
            {
                movements.Add(new CashMovementDto(o.CreatedAt, "Anticipo", customer, o.CashReceived, o.Id));
            }
        }

        // ── 3. Cobros de deuda en efectivo ───────────────────────────────────
        var sessionTo = session.ClosedAt ?? DateTime.UtcNow;
        var paidDebts = await _debts.GetPaidInRangeAsync(session.OpenedAt, sessionTo, ct);

        foreach (var d in paidDebts.Where(d => d.PaidWithMethod == PaymentMethod.Cash))
        {
            var desc = $"{d.CustomerName} · folio #{d.OrderFolio}";
            movements.Add(new CashMovementDto(d.PaidAt!.Value, "Cobro deuda", desc, d.Amount));
        }

        // ── 4. Gastos aprobados ──────────────────────────────────────────────
        var expenses = await _expenses.GetApprovedBySessionAsync(q.SessionId, ct);

        foreach (var e in expenses)
        {
            var at = e.ReviewedAt ?? e.RequestedAt;
            movements.Add(new CashMovementDto(at, "Gasto", e.Description, -e.Amount));
        }

        // ── 5. Retiros de caja ───────────────────────────────────────────────
        var withdrawals = await _withdrawals.GetBySessionAsync(q.SessionId, ct);

        foreach (var w in withdrawals)
        {
            var desc = string.IsNullOrWhiteSpace(w.Note) ? "Retiro de caja" : w.Note;
            movements.Add(new CashMovementDto(w.CreatedAt, "Retiro", desc, -w.Amount));
        }

        return Result.Ok(movements.OrderBy(m => m.At).ToList());
    }
}
