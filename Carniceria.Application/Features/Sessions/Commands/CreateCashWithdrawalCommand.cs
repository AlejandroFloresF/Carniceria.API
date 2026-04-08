using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Sessions.Commands;

public record CreateCashWithdrawalCommand(
    Guid   SessionId,
    decimal Amount,
    string? Note
) : IRequest<Result<Guid>>;

public class CreateCashWithdrawalHandler : IRequestHandler<CreateCashWithdrawalCommand, Result<Guid>>
{
    private readonly ISessionRepository        _sessions;
    private readonly ICashWithdrawalRepository _withdrawals;

    public CreateCashWithdrawalHandler(
        ISessionRepository sessions,
        ICashWithdrawalRepository withdrawals)
    {
        _sessions    = sessions;
        _withdrawals = withdrawals;
    }

    public async Task<Result<Guid>> Handle(CreateCashWithdrawalCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(cmd.SessionId, ct);
        if (session is null)
            return Result.Fail<Guid>("Sesión no encontrada.");
        if (session.Status == SessionStatus.Closed)
            return Result.Fail<Guid>("No se puede retirar de un turno cerrado.");

        try
        {
            session.DeductCash(cmd.Amount);
        }
        catch (DomainException ex)
        {
            return Result.Fail<Guid>(ex.Message);
        }

        var withdrawal = CashWithdrawal.Create(cmd.SessionId, session.CashierName, cmd.Amount, cmd.Note);
        await _sessions.SaveChangesAsync(ct);
        await _withdrawals.AddAsync(withdrawal, ct);

        return Result.Ok(withdrawal.Id);
    }
}
