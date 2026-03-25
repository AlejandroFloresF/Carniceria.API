using Carniceria.Application.Features.Products.Commands;
using Carniceria.Application.Features.Products.Queries;
using Carniceria.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Carniceria.API.Controllers;

public record UpdatePriceRequest(decimal NewPrice);
public record CreateProductRequest(string Name, string Category, decimal Price, string Unit);
public record UpdateProductRequest(string Name, string Category, decimal Price, string Unit);

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;
    public ProductsController(ISender mediator) => _mediator = mediator;

    // GET /api/products?search=...
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchProductsQuery(search));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // GET /api/products/all  (incluye inactivos, para gestión)
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var repo = HttpContext.RequestServices.GetRequiredService<IProductRepository>();
        var products = await repo.GetAllIncludingInactiveAsync();
        return Ok(products.Select(p => new {
            id           = p.Id,
            name         = p.Name,
            category     = p.Category,
            pricePerUnit = p.PricePerUnit,
            unit         = p.Unit,
            stockKg      = p.StockKg,
            isActive     = p.IsActive,
        }));
    }

    // GET /api/products/with-prices/:customerId
    [HttpGet("with-prices/{customerId:guid}")]
    public async Task<IActionResult> WithCustomerPrices(Guid customerId)
    {
        var result = await _mediator.Send(new GetProductsWithCustomerPricesQuery(customerId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // POST /api/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var result = await _mediator.Send(new CreateProductCommand(req.Name, req.Category, req.Price, req.Unit));
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    // PUT /api/products/:id
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest req)
    {
        var result = await _mediator.Send(new UpdateProductCommand(id, req.Name, req.Category, req.Price, req.Unit));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // PUT /api/products/:id/price  (mantiene compatibilidad con inventario)
    [HttpPut("{id:guid}/price")]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceRequest req)
    {
        var result = await _mediator.Send(new UpdateProductPriceCommand(id, req.NewPrice));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // PATCH /api/products/:id/toggle-active
    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _mediator.Send(new ToggleProductActiveCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    // DELETE /api/products/:id
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}
