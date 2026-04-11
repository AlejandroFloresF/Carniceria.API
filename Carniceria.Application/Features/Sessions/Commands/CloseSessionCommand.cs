using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;
namespace Carniceria.Application.Features.Sessions.Commands;
public record CloseSessionCommand(Guid SessionId, decimal ClosingCash) : IRequest<Result<bool>>;
public class CloseSessionHandler : IRequestHandler<CloseSessionCommand, Result<bool>>
{
    private readonly ISessionRepository _sessions;
    public CloseSessionHandler(ISessionRepository sessions) => _sessions = sessions;
    public async Task<Result<bool>> Handle(CloseSessionCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(cmd.SessionId, ct);
        if (session is null) return Result.Fail<bool>("Session not found.");
        try { session.Close(cmd.ClosingCash); }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
        await _sessions.SaveChangesAsync(ct);
        return Result.Ok(true);
    }
}
