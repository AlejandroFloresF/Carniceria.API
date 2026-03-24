using Carniceria.Application.Features.Products.Commands;
using Carniceria.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace Carniceria.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;
    public ProductsController(ISender mediator) => _mediator = mediator;
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchProductsQuery(search));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("with-prices/{customerId:guid}")]
    public async Task<IActionResult> WithCustomerPrices(Guid customerId)
    {
        var result = await _mediator.Send(new GetProductsWithCustomerPricesQuery(customerId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}/price")]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceRequest req)
    {
        var result = await _mediator.Send(new UpdateProductPriceCommand(id, req.NewPrice));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    public record UpdatePriceRequest(decimal NewPrice);
}
