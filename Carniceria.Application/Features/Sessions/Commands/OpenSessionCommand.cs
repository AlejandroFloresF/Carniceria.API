using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Sessions.Commands;
public record OpenSessionCommand(string CashierName, decimal OpeningCash) : IRequest<Result<CashierSessionDto>>;
public class OpenSessionHandler : IRequestHandler<OpenSessionCommand, Result<CashierSessionDto>>
{
    private readonly ISessionRepository _sessions;
    public OpenSessionHandler(ISessionRepository sessions) => _sessions = sessions;
    public async Task<Result<CashierSessionDto>> Handle(
    OpenSessionCommand cmd, CancellationToken ct)
    {
        var existing = await _sessions.GetOpenSessionByCashierAsync(cmd.CashierName, ct);
        if (existing is not null)
        {
            existing.Close(existing.OpeningCash); 
            await _sessions.SaveChangesAsync(ct);
        }

        var session = CashierSession.Open(cmd.CashierName, cmd.OpeningCash);
        await _sessions.AddAsync(session, ct);

        return Result.Ok(new CashierSessionDto(
            session.Id,
            session.CashierName,
            session.OpenedAt,        
            session.OpeningCash));
    }
}
