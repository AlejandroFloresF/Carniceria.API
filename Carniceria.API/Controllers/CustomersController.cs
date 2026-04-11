using System.ComponentModel.DataAnnotations;
using Carniceria.Application.Features.Customers.Commands;
using Carniceria.Application.Features.Customers.Queries;
using Carniceria.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

// ── Request records ───────────────────────────────────────────────────────────
public record CreateCustomerRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(20)]  string? Phone,
    [MaxLength(100)] string? Address,
    [Range(0, 100)]  decimal DiscountPercent,
    [MaxLength(7)]   string Color = "#6366f1",
    [MaxLength(10)]  string? Emoji = null,
    [MaxLength(1000)] string? Notes = null
);

public record UpdateCustomerRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(20)]  string? Phone,
    [MaxLength(100)] string? Address,
    [Range(0, 100)]  decimal DiscountPercent,
    [MaxLength(7)]   string Color = "#6366f1",
    [MaxLength(10)]  string? Emoji = null,
    [MaxLength(1000)] string? Notes = null
);

public record SetPriceRequest([Range(0.01, 9_999_999)] decimal CustomPrice);
public record PayDebtRequest([Required][MaxLength(20)] string PaymentMethod, [Range(0, 9_999_999)] decimal CashReceived);

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class CustomersController : ControllerBase
{
    private readonly ISender _mediator;
    public CustomersController(ISender mediator) => _mediator = mediator;

    // GET /api/customers?search=...
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchCustomersQuery(search));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // GET /api/customers/:id
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await _mediator.Send(new GetCustomerDetailQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
    // ← eliminado GetById duplicado

    // POST /api/customers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req)
    {
        var result = await _mediator.Send(
            new CreateCustomerCommand(
                req.Name, req.Phone, req.Address,
                req.DiscountPercent, req.Color, req.Emoji, req.Notes));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    // PUT /api/customers/:id
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest req)
    {
        var result = await _mediator.Send(
            new UpdateCustomerCommand(
                id, req.Name, req.Phone, req.Address,
                req.DiscountPercent, req.Color, req.Emoji, req.Notes));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    // PUT /api/customers/:customerId/prices/:productId
    [HttpPut("{customerId:guid}/prices/{productId:guid}")]
    public async Task<IActionResult> SetPrice(
        Guid customerId, Guid productId,
        [FromBody] SetPriceRequest req)
    {
        var result = await _mediator.Send(
            new SetCustomerProductPriceCommand(customerId, productId, req.CustomPrice));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // DELETE /api/customers/:customerId/prices/:productId
    [HttpDelete("{customerId:guid}/prices/{productId:guid}")]
    public async Task<IActionResult> DeletePrice(Guid customerId, Guid productId)
    {
        var prices = HttpContext.RequestServices
            .GetRequiredService<ICustomerProductPriceRepository>();
        await prices.DeleteAsync(customerId, productId);
        await prices.SaveChangesAsync();
        return Ok();
    }

    // POST /api/customers/debts/:debtId/pay
    [HttpPost("debts/{debtId:guid}/pay")]
    public async Task<IActionResult> PayDebt(Guid debtId, [FromBody] PayDebtRequest req)
    {
        if (!Enum.TryParse<Carniceria.Domain.Entities.PaymentMethod>(req.PaymentMethod, out var method))
            return BadRequest(new { error = "Invalid payment method." });

        var result = await _mediator.Send(new MarkDebtAsPaidCommand(debtId, method, req.CashReceived));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
    // DELETE /api/customers/:id
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}