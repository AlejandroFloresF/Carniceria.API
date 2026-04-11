using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carniceria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _tickets;
    private readonly IOrderRepository _orders;

    public TicketsController(
        ITicketRepository tickets,
        IOrderRepository orders)
    {
        _tickets = tickets;
        _orders = orders;
    }

    // GET /api/tickets/:id
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ticket = await _tickets.GetByIdAsync(id);
        if (ticket is null) return NotFound();
        var order = await _orders.GetByIdAsync(ticket.OrderId);
        return Ok(MapToDto(ticket, order));
    }

    // GET /api/tickets/folio/:folio
    [HttpGet("folio/{folio}")]
    public async Task<IActionResult> GetByFolio(string folio)
    {
        var ticket = await _tickets.GetByFolioAsync(folio);
        if (ticket is null) return NotFound();
        var order = await _orders.GetByIdAsync(ticket.OrderId);
        return Ok(MapToDto(ticket, order));
    }

    // GET /api/tickets/by-order/:orderId
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var ticket = await _tickets.GetByOrderIdAsync(orderId);
        if (ticket is null) return NotFound();
        var order = await _orders.GetByIdAsync(orderId);
        return Ok(MapToDto(ticket, order));
    }

    private static object MapToDto(Ticket t, Order? o) => new
    {
        id = t.Id,
        folio = t.Folio,
        orderId = t.OrderId,
        issuedAt = t.CreatedAt,
        cashierName = t.CashierName,
        shopName = t.ShopName,
        customerName = t.CustomerName,
        subtotal = t.Subtotal,
        discountAmount = t.DiscountAmount,
        total = t.Total,
        cashReceived = t.CashReceived,
        change = t.Change,
        paymentMethod = t.PaymentMethod.ToString(),
        items = o?.Items.Select(i => new
        {
            productId = i.ProductId,
            productName = i.ProductName,
            quantity = i.Quantity,
            unit = i.Unit,
            unitPrice = i.UnitPrice,
            total = i.LineTotal,
        }) ?? Enumerable.Empty<object>(),
    };
}