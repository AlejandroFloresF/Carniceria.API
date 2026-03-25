using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

public record SaleRecordDto(
    string Folio, DateTime CreatedAt, string CustomerName,
    string PaymentMethod, decimal Total, decimal DiscountAmount,
    string CashierName, bool IsDebtPayment,
    decimal AdvancePayment, decimal PendingDebt
);

public record GetOrdersListQuery(DateTime From, DateTime To, Guid? SessionId)
    : IRequest<Result<List<SaleRecordDto>>>;

public class GetOrdersListHandler : IRequestHandler<GetOrdersListQuery, Result<List<SaleRecordDto>>>
{
    private readonly IDashboardRepository _dashboard;
    private readonly ITicketRepository _tickets;
    private readonly ICustomerDebtRepository _debts;

    public GetOrdersListHandler(IDashboardRepository dashboard, ITicketRepository tickets, ICustomerDebtRepository debts)
    {
        _dashboard = dashboard;
        _tickets   = tickets;
        _debts     = debts;
    }

    public async Task<Result<List<SaleRecordDto>>> Handle(
        GetOrdersListQuery q, CancellationToken ct)
    {
        var orders = await _dashboard.GetOrdersInRangeAsync(q.From, q.To, q.SessionId, ct);
        var completed = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .ToList();

        var result = new List<SaleRecordDto>();

        foreach (var o in completed)
        {
            var ticket = await _tickets.GetByOrderIdAsync(o.Id, ct);
            var isPayLater  = o.PaymentMethod == PaymentMethod.PayLater;
            var advanceAmt  = isPayLater ? o.CashReceived : 0m;
            var pendingAmt  = isPayLater ? o.Total - o.CashReceived : 0m;
            result.Add(new SaleRecordDto(
                ticket?.Folio ?? "—",
                o.CreatedAt,
                o.CustomerName ?? "Público General",
                o.PaymentMethod.ToString(),
                o.Total,
                o.DiscountAmount,
                ticket?.CashierName ?? "—",
                false,
                advanceAmt,
                pendingAmt
            ));
        }

        // Cobros de deudas pagados en el período (no filtran por sesión)
        var paidDebts = await _debts.GetPaidInRangeAsync(q.From, q.To, ct);
        foreach (var d in paidDebts)
        {
            result.Add(new SaleRecordDto(
                d.OrderFolio,
                d.PaidAt!.Value,
                d.CustomerName,
                d.PaidWithMethod?.ToString() ?? "Cash",
                d.Amount,
                0m,
                "—",
                true,
                0m,
                0m
            ));
        }

        result = result.OrderByDescending(r => r.CreatedAt).ToList();
        return Result.Ok(result);
    }
}