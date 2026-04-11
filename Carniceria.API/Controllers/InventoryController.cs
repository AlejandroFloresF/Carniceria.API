using Carniceria.Application.Common;
using Carniceria.Application.Features.Inventory.Commands;
using Carniceria.Application.Features.Inventory.Queries;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace Carniceria.API.Controllers;

public record RegisterEntryRequest(
    [Required] Guid ProductId,
    [Range(0.001, 99999)] decimal QuantityKg,
    [Range(0, 9_999_999)] decimal CostPerKg,
    EntrySource Source,
    [MaxLength(500)] string? Notes);

public record RegisterWasteRequest(
    [Required] Guid ProductId,
    [Range(0.001, 99999)] decimal QuantityKg,
    WasteReason Reason,
    [MaxLength(500)] string? Notes);

public record SetAlertRequest([Range(0, 99999)] decimal MinimumStockKg);

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class InventoryController : ControllerBase
{
    private readonly ISender _mediator;
    public InventoryController(ISender mediator) => _mediator = mediator;

    // Stock status de todos los productos con alertas
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var result = await _mediator.Send(new GetStockStatusQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // Historial de movimientos de un producto
    [HttpGet("movements/{productId:guid}")]
    public async Task<IActionResult> Movements(
        Guid productId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _mediator.Send(new GetMovementsQuery(productId, from, to));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // Registrar entrada de inventario
    [HttpPost("entries")]
    public async Task<IActionResult> RegisterEntry([FromBody] RegisterEntryRequest req)
    {
        var result = await _mediator.Send(
            new RegisterEntryCommand(req.ProductId, req.QuantityKg, req.CostPerKg, req.Source, req.Notes));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // Registrar merma
    [HttpPost("waste")]
    public async Task<IActionResult> RegisterWaste([FromBody] RegisterWasteRequest req)
    {
        var result = await _mediator.Send(
            new RegisterWasteCommand(req.ProductId, req.QuantityKg, req.Reason, req.Notes));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // Configurar alerta de stock mínimo
    [HttpPut("alerts/{productId:guid}")]
    public async Task<IActionResult> SetAlert(Guid productId, [FromBody] SetAlertRequest req)
    {
        var inventory = HttpContext.RequestServices.GetRequiredService<IInventoryRepository>();
        var existing = await inventory.GetAlertByProductAsync(productId);

        if (existing is not null)
        {
            existing.Update(req.MinimumStockKg);
            await inventory.SaveChangesAsync();
        }
        else
        {
            var alert = StockAlert.Create(productId, req.MinimumStockKg);
            await inventory.AddAlertAsync(alert);
            await inventory.SaveChangesAsync();
        }

        return Ok();
    }
}