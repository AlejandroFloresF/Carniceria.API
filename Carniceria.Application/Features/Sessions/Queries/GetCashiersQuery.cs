using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Sessions.Queries;

public record GetCashiersQuery : IRequest<Result<List<string>>>;

public class GetCashiersHandler : IRequestHandler<GetCashiersQuery, Result<List<string>>>
{
    private readonly ISessionRepository _sessions;
    public GetCashiersHandler(ISessionRepository sessions) => _sessions = sessions;

    public async Task<Result<List<string>>> Handle(GetCashiersQuery q, CancellationToken ct)
    {
        var cashiers = await _sessions.GetDistinctCashiersAsync(ct);
        return Result.Ok(cashiers);
    }
}