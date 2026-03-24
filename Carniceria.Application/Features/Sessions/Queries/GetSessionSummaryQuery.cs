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
    public GetSessionSummaryHandler(ISessionRepository sessions) => _sessions = sessions;
    public async Task<Result<SessionSummaryDto>> Handle(GetSessionSummaryQuery q, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(q.SessionId, ct);
        if (session is null) return Result.Fail<SessionSummaryDto>("Session not found.");
        var orders = await _sessions.GetOrdersAsync(q.SessionId, ct);
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        return Result.Ok(new SessionSummaryDto(
            session.Id, session.CashierName, session.OpenedAt, session.ClosedAt,
            completed.Count, completed.Sum(o => o.Total),
            completed.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total),
            completed.Where(o => o.PaymentMethod == PaymentMethod.Card).Sum(o => o.Total),
            completed.Where(o => o.PaymentMethod == PaymentMethod.Transfer).Sum(o => o.Total),
            completed.Sum(o => o.DiscountAmount), session.OpeningCash,
            session.OpeningCash + completed.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total)));
    }
}
