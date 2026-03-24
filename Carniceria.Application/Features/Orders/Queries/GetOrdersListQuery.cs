using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

public record SaleRecordDto(
    string Folio, DateTime CreatedAt, string CustomerName,
    string PaymentMethod, decimal Total, decimal DiscountAmount,
    string CashierName
);

public record GetOrdersListQuery(DateTime From, DateTime To, Guid? SessionId)
    : IRequest<Result<List<SaleRecordDto>>>;

public class GetOrdersListHandler : IRequestHandler<GetOrdersListQuery, Result<List<SaleRecordDto>>>
{
    private readonly IDashboardRepository _dashboard;
    private readonly ITicketRepository _tickets;

    public GetOrdersListHandler(IDashboardRepository dashboard, ITicketRepository tickets)
    {
        _dashboard = dashboard;
        _tickets = tickets;
    }

    public async Task<Result<List<SaleRecordDto>>> Handle(
        GetOrdersListQuery q, CancellationToken ct)
    {
        var orders = await _dashboard.GetOrdersInRangeAsync(q.From, q.To, q.SessionId, ct);
        var completed = orders
            .Where(o => o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var result = new List<SaleRecordDto>();
        foreach (var o in completed)
        {
            var ticket = await _tickets.GetByOrderIdAsync(o.Id, ct);
            result.Add(new SaleRecordDto(
                ticket?.Folio ?? "—",
                o.CreatedAt,
                o.CustomerName ?? "Público General",
                o.PaymentMethod.ToString(),
                o.Total,
                o.DiscountAmount,
                ticket?.CashierName ?? "—"
            ));
        }

        return Result.Ok(result);
    }
}