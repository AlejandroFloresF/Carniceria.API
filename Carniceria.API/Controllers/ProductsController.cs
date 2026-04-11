using System.ComponentModel.DataAnnotations;
using Carniceria.Application.Features.Products.Commands;
using Carniceria.Application.Features.Products.Queries;
using Carniceria.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

public record UpdatePriceRequest([Range(0.01, 9_999_999)] decimal NewPrice);
public record CreateProductRequest(
    [Required][MaxLength(100)] string Name,
    [Required][MaxLength(50)]  string Category,
    [Range(0.01, 9_999_999)]   decimal Price,
    [Required][MaxLength(10)]  string Unit,
    [MaxLength(100)]           string? Barcode = null
);
public record UpdateProductRequest(
    [Required][MaxLength(100)] string Name,
    [Required][MaxLength(50)]  string Category,
    [Range(0.01, 9_999_999)]   decimal Price,
    [Required][MaxLength(10)]  string Unit,
    [MaxLength(100)]           string? Barcode = null
);

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
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
            barcode      = p.Barcode,
        }));
    }

    // GET /api/products/by-barcode/{code}  — used by barcode scanner
    [HttpGet("by-barcode/{code}")]
    public async Task<IActionResult> ByBarcode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100)
            return BadRequest(new { error = "Invalid barcode" });

        var repo = HttpContext.RequestServices.GetRequiredService<IProductRepository>();
        var p = await repo.GetByBarcodeAsync(code);
        if (p is null) return NotFound(new { error = "Producto no encontrado" });

        return Ok(new {
            id           = p.Id,
            name         = p.Name,
            category     = p.Category,
            pricePerUnit = p.PricePerUnit,
            effectivePrice = p.PricePerUnit,
            unit         = p.Unit,
            stockKg      = p.StockKg,
            barcode      = p.Barcode,
        });
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
        var result = await _mediator.Send(new CreateProductCommand(req.Name, req.Category, req.Price, req.Unit, req.Barcode));
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(new { error = result.Error });
    }

    // PUT /api/products/:id
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest req)
    {
        var result = await _mediator.Send(new UpdateProductCommand(id, req.Name, req.Category, req.Price, req.Unit, req.Barcode));
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

    // GET /api/products/:id/price-history
    [HttpGet("{id:guid}/price-history")]
    public async Task<IActionResult> PriceHistory(Guid id)
    {
        var result = await _mediator.Send(new GetProductPriceHistoryQuery(id));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // DELETE /api/products/:id
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}
