using Carniceria.Application.Features.CustomerOrders.Commands;
using Carniceria.Application.Features.CustomerOrders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace Carniceria.API.Controllers;

public record CreateCustomerOrderRequest(
    [MaxLength(50)] string Recurrence,
    DateTime NextDeliveryDate,
    [Required][MinLength(1)] List<CreateCustomerOrderItemInput> Items,
    [MaxLength(500)] string? Notes = null
);

[ApiController]
[Route("api/customers/{customerId:guid}/orders")]
[EnableRateLimiting("api")]
public class CustomerOrdersController : ControllerBase
{
    private readonly ISender _mediator;
    public CustomerOrdersController(ISender mediator) => _mediator = mediator;

    // GET /api/customers/{customerId}/orders
    [HttpGet]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        var result = await _mediator.Send(new GetCustomerOrdersQuery(customerId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST /api/customers/{customerId}/orders
    [HttpPost]
    public async Task<IActionResult> Create(Guid customerId, [FromBody] CreateCustomerOrderRequest req)
    {
        var result = await _mediator.Send(
            new CreateCustomerOrderCommand(
                customerId, req.Recurrence, req.NextDeliveryDate,
                req.Items, req.Notes));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // PUT /api/customers/{customerId}/orders/{orderId}
    [HttpPut("{orderId:guid}")]
    public async Task<IActionResult> Update(
        Guid customerId, Guid orderId,
        [FromBody] CreateCustomerOrderRequest req)
    {
        var result = await _mediator.Send(
            new UpdateCustomerOrderCommand(
                orderId, req.Recurrence, req.NextDeliveryDate,
                req.Items, req.Notes));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // DELETE /api/customers/{customerId}/orders/{orderId}
    [HttpDelete("{orderId:guid}")]
    public async Task<IActionResult> Delete(Guid customerId, Guid orderId)
    {
        var result = await _mediator.Send(new DeleteCustomerOrderCommand(orderId));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // POST /api/customers/{customerId}/orders/{orderId}/fulfill
    [HttpPost("{orderId:guid}/fulfill")]
    public async Task<IActionResult> Fulfill(Guid customerId, Guid orderId)
    {
        var result = await _mediator.Send(new FulfillCustomerOrderCommand(orderId));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}

// Separate controller for the stock-alerts endpoint (no customerId in route)
[ApiController]
[Route("api/customers/orders")]
public class CustomerOrderAlertsController : ControllerBase
{
    private readonly ISender _mediator;
    public CustomerOrderAlertsController(ISender mediator) => _mediator = mediator;

    // GET /api/customers/orders/stock-alerts?days=3
    [HttpGet("stock-alerts")]
    public async Task<IActionResult> StockAlerts([FromQuery] int days = 3)
    {
        var result = await _mediator.Send(new GetStockShortagesQuery(days));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // GET /api/customers/orders/today
    [HttpGet("today")]
    public async Task<IActionResult> Today()
    {
        var result = await _mediator.Send(new GetTodayOrdersQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
