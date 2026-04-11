using Carniceria.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("api")]
public class DashboardController : ControllerBase
{
    private readonly ISender _mediator;
    public DashboardController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? sessionId)
    {
        var now = DateTime.UtcNow;
        var fromUtc = from.HasValue
            ? DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc)
            : now.Date;

        var toUtc = to.HasValue
            ? DateTime.SpecifyKind(to.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
            : now.Date.AddDays(1).AddTicks(-1);

        var result = await _mediator.Send(new GetDashboardQuery(fromUtc, toUtc, sessionId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}