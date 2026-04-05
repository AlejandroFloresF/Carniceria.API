using Carniceria.Application.Common;
using Carniceria.Application.Features.Orders.Commands;
using Carniceria.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Carniceria.API.Controllers;

public record CreateOrderRequest(
    Guid CashierSessionId,
    List<OrderItemInputDto> Items,
    decimal DiscountPercent,
    PaymentMethod PaymentMethod,
    decimal CashReceived,
    Guid? CustomerId,
    string? DebtNote = null,
    decimal AdvancePayment = 0,
    string? AdvancePaymentMethod = null,
    Guid? SourceCustomerOrderId = null,
    string? SecondaryPaymentMethod = null,
    decimal SecondaryAmount = 0
);

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;
    public OrdersController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        PaymentMethod? advMethod = null;
        if (!string.IsNullOrEmpty(req.AdvancePaymentMethod) &&
            Enum.TryParse<PaymentMethod>(req.AdvancePaymentMethod, out var parsedAdv))
            advMethod = parsedAdv;

        PaymentMethod? secondaryMethod = null;
        if (!string.IsNullOrEmpty(req.SecondaryPaymentMethod) &&
            Enum.TryParse<PaymentMethod>(req.SecondaryPaymentMethod, out var parsedSecondary))
            secondaryMethod = parsedSecondary;

        var result = await _mediator.Send(new CreateOrderCommand(
            req.CashierSessionId,
            req.Items,
            req.DiscountPercent,
            req.PaymentMethod,
            req.CashReceived,
            req.CustomerId,
            req.DebtNote,
            req.AdvancePayment,
            advMethod,
            req.SourceCustomerOrderId,
            secondaryMethod,
            req.SecondaryAmount
        ));
        return result.IsSuccess
            ? CreatedAtAction(nameof(Create), result.Value)
            : BadRequest(new { error = result.Error });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("list")]
    public async Task<IActionResult> List(
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromQuery] Guid? sessionId)
    {
        var result = await _mediator.Send(new GetOrdersListQuery(
            from ?? DateTime.UtcNow.Date,
            to ?? DateTime.UtcNow,
            sessionId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}