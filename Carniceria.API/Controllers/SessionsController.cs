using Carniceria.Application.Features.Sessions.Commands;
using Carniceria.Application.Features.Sessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Carniceria.API.Controllers;
public record OpenSessionRequest(string CashierName, decimal OpeningCash);
public record CloseSessionRequest(decimal ClosingCash);
[ApiController]
[Route("api/[controller]")]
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

    [HttpGet("cashiers")]
    public async Task<IActionResult> GetCashiers()
    {
        var result = await _mediator.Send(new GetCashiersQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
