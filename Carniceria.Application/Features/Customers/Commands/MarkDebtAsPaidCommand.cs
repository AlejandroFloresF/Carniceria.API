using Carniceria.Domain.Common;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Commands;

public record MarkDebtAsPaidCommand(Guid DebtId) : IRequest<Result<bool>>;

public class MarkDebtAsPaidHandler : IRequestHandler<MarkDebtAsPaidCommand, Result<bool>>
{
    private readonly ICustomerDebtRepository _debts;
    public MarkDebtAsPaidHandler(ICustomerDebtRepository debts) => _debts = debts;

    public async Task<Result<bool>> Handle(MarkDebtAsPaidCommand cmd, CancellationToken ct)
    {
        var debt = await _debts.GetByIdAsync(cmd.DebtId, ct);
        if (debt is null) return Result.Fail<bool>("Debt not found.");

        try
        {
            debt.MarkAsPaid();
            await _debts.SaveChangesAsync(ct);
            return Result.Ok(true);
        }
        catch (DomainException ex) { return Result.Fail<bool>(ex.Message); }
    }
}