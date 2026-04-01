using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

namespace Carniceria.Application.Features.Customers.Commands;

public record MarkDebtAsPaidCommand(
    Guid DebtId,
    PaymentMethod PaymentMethod,
    decimal CashReceived
) : IRequest<Result<TicketDto>>;

public class MarkDebtAsPaidHandler : IRequestHandler<MarkDebtAsPaidCommand, Result<TicketDto>>
{
    private readonly ICustomerDebtRepository _debts;
    private readonly ITicketRepository _tickets;
    private readonly IOrderRepository _orders;
    private readonly ISessionRepository _sessions;

    public MarkDebtAsPaidHandler(
        ICustomerDebtRepository debts,
        ITicketRepository tickets,
        IOrderRepository orders,
        ISessionRepository sessions)
    {
        _debts    = debts;
        _tickets  = tickets;
        _orders   = orders;
        _sessions = sessions;
    }

    public async Task<Result<TicketDto>> Handle(MarkDebtAsPaidCommand cmd, CancellationToken ct)
    {
        var debt = await _debts.GetByIdAsync(cmd.DebtId, ct);
        if (debt is null) return Result.Fail<TicketDto>("Debt not found.");

        try
        {
            debt.MarkAsPaid(cmd.PaymentMethod, cmd.CashReceived);
            await _debts.SaveChangesAsync(ct);
        }
        catch (DomainException ex) { return Result.Fail<TicketDto>(ex.Message); }

        // Si se pagó en efectivo, actualiza CurrentCash de la sesión abierta
        if (cmd.PaymentMethod == PaymentMethod.Cash)
        {
            var session = await _sessions.GetAnyOpenSessionAsync(ct);
            if (session is not null)
            {
                session.AddCash(debt.Amount);
                await _sessions.SaveChangesAsync(ct);
            }
        }

        // Obtiene el ticket original para construir el recibo de pago
        var ticket = await _tickets.GetByOrderIdAsync(debt.OrderId, ct);
        var order  = await _orders.GetByIdAsync(debt.OrderId, ct);

        if (ticket is null || order is null)
            return Result.Fail<TicketDto>("Original ticket not found.");

        var change = cmd.PaymentMethod == PaymentMethod.Cash
            ? Math.Floor(cmd.CashReceived - debt.Amount)
            : 0m;

        var receipt = new TicketDto(
            ticket.Id,
            ticket.Folio,
            ticket.OrderId,
            DateTime.UtcNow,
            ticket.CashierName,
            ticket.ShopName,
            order.Items.Select(i => new TicketItemDto(
                i.ProductId, i.ProductName, i.Quantity, i.Unit, i.UnitPrice, i.LineTotal
            )).ToList(),
            ticket.Subtotal,
            ticket.DiscountAmount,
            ticket.TaxAmount,
            debt.Amount,
            cmd.PaymentMethod == PaymentMethod.Cash ? cmd.CashReceived : debt.Amount,
            change,
            cmd.PaymentMethod,
            debt.CustomerName
        );

        return Result.Ok(receipt);
    }
}
