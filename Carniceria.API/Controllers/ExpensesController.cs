using System.ComponentModel.DataAnnotations;
using Carniceria.Application.Features.Expenses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

public record CreateScheduledExpenseRequest(
    [Required][MaxLength(100)] string Name,
    [Range(0.01, 9_999_999)]   decimal Amount,
    [Required][MaxLength(50)]  string Category,
    [Required][MaxLength(20)]  string Recurrence,
    DateTime NextDueDate,
    [Range(1, 90)] int AlertDaysBefore = 3,
    [MaxLength(300)] string? Description = null
);

public record UpdateScheduledExpenseRequest(
    [Required][MaxLength(100)] string Name,
    [Range(0.01, 9_999_999)]   decimal Amount,
    [Required][MaxLength(50)]  string Category,
    [Required][MaxLength(20)]  string Recurrence,
    DateTime NextDueDate,
    [Range(1, 90)] int AlertDaysBefore,
    [MaxLength(300)] string? Description
);

public record CreateExpenseRequestBody(
    [Required][MaxLength(200)] string Description,
    [Range(0.01, 9_999_999)]   decimal Amount,
    [Required][MaxLength(50)]  string Category,
    [Required][MaxLength(100)] string RequestedBy,
    Guid? SessionId,
    Guid? ScheduledExpenseId,
    [MaxLength(300)] string? Notes
);

public record ReviewExpenseRequestBody(
    bool Approved,
    [Required][MaxLength(100)] string ReviewedBy,
    [MaxLength(300)] string? DenyReason
);

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class ExpensesController : ControllerBase
{
    private readonly ISender _mediator;
    public ExpensesController(ISender mediator) => _mediator = mediator;

    // GET /api/expenses/scheduled
    [HttpGet("scheduled")]
    public async Task<IActionResult> GetScheduled()
    {
        var result = await _mediator.Send(new GetScheduledExpensesQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST /api/expenses/scheduled
    [HttpPost("scheduled")]
    public async Task<IActionResult> CreateScheduled([FromBody] CreateScheduledExpenseRequest req)
    {
        var result = await _mediator.Send(new CreateScheduledExpenseCommand(
            req.Name, req.Amount, req.Category, req.Recurrence,
            req.NextDueDate, req.AlertDaysBefore, req.Description));
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    // PUT /api/expenses/scheduled/:id
    [HttpPut("scheduled/{id:guid}")]
    public async Task<IActionResult> UpdateScheduled(Guid id, [FromBody] UpdateScheduledExpenseRequest req)
    {
        var result = await _mediator.Send(new UpdateScheduledExpenseCommand(
            id, req.Name, req.Amount, req.Category, req.Recurrence,
            req.NextDueDate, req.AlertDaysBefore, req.Description));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // DELETE /api/expenses/scheduled/:id
    [HttpDelete("scheduled/{id:guid}")]
    public async Task<IActionResult> DeleteScheduled(Guid id)
    {
        var result = await _mediator.Send(new DeleteScheduledExpenseCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // PATCH /api/expenses/scheduled/:id/toggle
    [HttpPatch("scheduled/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleScheduled(Guid id)
    {
        var result = await _mediator.Send(new ToggleScheduledExpenseCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // GET /api/expenses/requests?status=Approved&from=...&to=...
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] string? status, [FromQuery] string? requestedBy, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _mediator.Send(new GetExpenseRequestsQuery(status, requestedBy, from, to));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST /api/expenses/requests
    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateExpenseRequestBody req)
    {
        var result = await _mediator.Send(new CreateExpenseRequestCommand(
            req.Description, req.Amount, req.Category,
            req.RequestedBy, req.SessionId, req.ScheduledExpenseId, req.Notes));
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    // PUT /api/expenses/requests/:id/review
    [HttpPut("requests/{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewExpenseRequestBody req)
    {
        var result = await _mediator.Send(new ReviewExpenseRequestCommand(id, req.Approved, req.ReviewedBy, req.DenyReason));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // GET /api/expenses/notifications?cashierName=Ana
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] string? cashierName)
    {
        var result = await _mediator.Send(new GetExpenseNotificationsQuery(cashierName));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
