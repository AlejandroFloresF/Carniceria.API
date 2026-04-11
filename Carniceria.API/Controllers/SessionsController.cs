using System.ComponentModel.DataAnnotations;
using Carniceria.Application.Features.Sessions.Commands;
using Carniceria.Application.Features.Sessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

public record OpenSessionRequest([Required][MaxLength(100)] string CashierName, [Range(0, 9_999_999)] decimal OpeningCash);
public record CloseSessionRequest([Range(0, 9_999_999)] decimal ClosingCash);
public record CashWithdrawalRequest([Range(0.01, 9_999_999)] decimal Amount, [MaxLength(300)] string? Note);

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class SessionsController : ControllerBase
{
    private readonly ISender _mediator;
    public SessionsController(ISender mediator) => _mediator = mediator;
    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenSessionRequest req)
    {
        var result = await _mediator.Send(new OpenSessionCommand(req.CashierName, req.OpeningCash));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseSessionRequest req)
    {
        var result = await _mediator.Send(new CloseSessionCommand(id, req.ClosingCash));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> Summary(Guid id)
    {
        var result = await _mediator.Send(new GetSessionSummaryQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> Movements(Guid id)
    {
        var result = await _mediator.Send(new GetSessionMovementsQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] CashWithdrawalRequest req)
    {
        var result = await _mediator.Send(new CreateCashWithdrawalCommand(id, req.Amount, req.Note));
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("cashiers")]
    public async Task<IActionResult> GetCashiers()
    {
        var result = await _mediator.Send(new GetCashiersQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _mediator.Send(new GetActiveSessionsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
