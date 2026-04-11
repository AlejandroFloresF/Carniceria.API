using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Sessions.Queries;

public record GetActiveSessionsQuery : IRequest<Result<List<ActiveSessionDto>>>;

public class GetActiveSessionsHandler : IRequestHandler<GetActiveSessionsQuery, Result<List<ActiveSessionDto>>>
{
    private readonly ISessionRepository _sessions;

    public GetActiveSessionsHandler(ISessionRepository sessions) => _sessions = sessions;

    public async Task<Result<List<ActiveSessionDto>>> Handle(GetActiveSessionsQuery _, CancellationToken ct)
    {
        var openSessions = await _sessions.GetAllOpenSessionsAsync(ct);

        var result = new List<ActiveSessionDto>();

        foreach (var session in openSessions)
        {
            var orders    = await _sessions.GetOrdersAsync(session.Id, ct);
            var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

            var totalSales    = completed.Sum(o => o.Total);
            var totalCash     = completed.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total);
            var totalCard     = completed.Where(o => o.PaymentMethod == PaymentMethod.Card).Sum(o => o.Total);
            var totalTransfer = completed.Where(o => o.PaymentMethod == PaymentMethod.Transfer).Sum(o => o.Total);
            var totalPayLater = completed.Where(o => o.PaymentMethod == PaymentMethod.PayLater).Sum(o => o.Total);
            var totalDisc     = completed.Sum(o => o.DiscountAmount);

            // Última venta: descripción resumida con los primeros productos
            var lastOrder = completed.OrderByDescending(o => o.CreatedAt).FirstOrDefault();
            string? lastDesc  = null;
            decimal lastAmt   = 0;
            if (lastOrder is not null)
            {
                var names = lastOrder.Items.Take(2).Select(i => i.ProductName).ToList();
                if (lastOrder.Items.Count > 2)
                    names.Add($"+{lastOrder.Items.Count - 2} más");
                lastDesc = string.Join(", ", names);
                lastAmt  = lastOrder.Total;
            }

            result.Add(new ActiveSessionDto(
                session.Id,
                session.CashierName,
                session.OpenedAt,
                session.CurrentCash,
                completed.Count,
                totalSales,
                totalCash,
                totalCard,
                totalTransfer,
                totalPayLater,
                totalDisc,
                lastOrder?.CreatedAt,
                lastDesc,
                lastAmt
            ));
        }

        return Result.Ok(result);
    }
}
