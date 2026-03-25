using Carniceria.Application.Features.Customers.Commands;
using Carniceria.Application.Features.Customers.Queries;
using Carniceria.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Carniceria.API.Controllers;

// ── Request records ───────────────────────────────────────────────────────────
public record CreateCustomerRequest(
    string Name,
    string? Phone,
    string? Address,          // ← Address no Email
    decimal DiscountPercent,
    string Color = "#6366f1",
    string? Emoji = null
);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? Address,          // ← Address no Email
    decimal DiscountPercent,
    string Color = "#6366f1",
    string? Emoji = null
);

public record SetPriceRequest(decimal CustomPrice);
public record PayDebtRequest(string PaymentMethod, decimal CashReceived);

[ApiController]
[Route("api/[controller]")]
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
                req.DiscountPercent, req.Color, req.Emoji));
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
                req.DiscountPercent, req.Color, req.Emoji));
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