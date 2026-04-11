using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;

namespace Carniceria.Application.Features.Orders.Commands;

public record CreateOrderCommand(
    Guid CashierSessionId,
    List<OrderItemInputDto> Items,
    decimal DiscountPercent,
    PaymentMethod PaymentMethod,
    decimal CashReceived,
    Guid? CustomerId,
    string? DebtNote = null,
    decimal AdvancePayment = 0,
    PaymentMethod? AdvancePaymentMethod = null,
    Guid? SourceCustomerOrderId = null,
    PaymentMethod? SecondaryPaymentMethod = null,
    decimal SecondaryAmount = 0
) : IRequest<Result<TicketDto>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<TicketDto>>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ITicketRepository _tickets;
    private readonly ISessionRepository _sessions;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerDebtRepository _debts;
    private readonly ICustomerProductPriceRepository _prices;
    private readonly string _shopName;

    public CreateOrderHandler(
        IOrderRepository orders,
        IProductRepository products,
        ITicketRepository tickets,
        ISessionRepository sessions,
        ICustomerRepository customers,
        ICustomerDebtRepository debts,
        ICustomerProductPriceRepository prices,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _orders = orders;
        _products = products;
        _tickets = tickets;
        _sessions = sessions;
        _customers = customers;
        _debts = debts;
        _prices = prices;
        _shopName = config["App:ShopName"] ?? "GRADILLA 100% EST 1938";
    }

    public async Task<Result<TicketDto>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        // ── 1. Valida sesión ─────────────────────────────────
        var session = await _sessions.GetByIdAsync(cmd.CashierSessionId, ct);
        if (session is null)
            return Result.Fail<TicketDto>("Cashier session not found.");
        if (session.Status == SessionStatus.Closed)
            return Result.Fail<TicketDto>("Cannot create order on a closed session.");

        // ── 2. Carga cliente si aplica ───────────────────────
        Domain.Entities.Customer? customer = null;
        Dictionary<Guid, decimal> customPriceMap = new();

        if (cmd.CustomerId.HasValue)
        {
            customer = await _customers.GetByIdAsync(cmd.CustomerId.Value, ct);
            if (customer is not null)
            {
                customer.TouchActivity();
                // Carga precios especiales del cliente para este pedido
                var customPrices = await _prices.GetByCustomerAsync(customer.Id, ct);
                customPriceMap = customPrices.ToDictionary(p => p.ProductId, p => p.CustomPrice);
            }
        }

        // ── 3. Construye la orden ────────────────────────────
        var order = Order.Create(cmd.CashierSessionId);

        foreach (var item in cmd.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product is null)
                return Result.Fail<TicketDto>($"Product {item.ProductId} not found.");

            if (product.StockKg < item.Quantity)
                return Result.Fail<TicketDto>($"Stock insuficiente para {product.Name}. Disponible: {product.StockKg:F3} kg.");

            // Si el cliente tiene precio especial para este producto, lo aplica
            if (customPriceMap.TryGetValue(product.Id, out var customPrice))
                order.AddItemWithCustomPrice(product, item.Quantity, customPrice);
            else
                order.AddItem(product, item.Quantity);

            // Deducción atómica en DB: protege contra ventas simultáneas del mismo producto
            var deducted = await _products.DeductStockAtomicAsync(product.Id, item.Quantity, ct);
            if (!deducted)
                return Result.Fail<TicketDto>($"Stock insuficiente para {product.Name} (modificado por otra venta simultánea).");
        }

        // ── 4. Aplica descuento y asigna cliente ─────────────
        // El frontend siempre envía cmd.DiscountPercent con el descuento correcto
        // (ya sea el del cliente o el manual). Lo aplicamos directamente.
        if (customer is not null)
            order.AssignCustomer(customer);

        if (cmd.DiscountPercent > 0)
            order.ApplyDiscount(cmd.DiscountPercent);

        // ── 4b. Marca origen pedido si aplica ───────────────
        if (cmd.SourceCustomerOrderId.HasValue)
            order.MarkAsFromCustomerOrder(cmd.SourceCustomerOrderId.Value);

        if (cmd.PaymentMethod == PaymentMethod.PayLater)
        {
            if (customer is null)
                return Result.Fail<TicketDto>("PayLater requires a registered customer.");

            try
            {
                order.ConfirmPayLater(customer.Id, customer.Name, cmd.AdvancePayment, cmd.AdvancePaymentMethod);
            }
            catch (DomainException ex)
            {
                return Result.Fail<TicketDto>(ex.Message);
            }
        }
        else if (cmd.SecondaryPaymentMethod.HasValue && cmd.SecondaryAmount > 0)
        {
            try
            {
                order.ConfirmSplit(cmd.CashReceived, cmd.SecondaryPaymentMethod.Value, cmd.SecondaryAmount);
            }
            catch (DomainException ex)
            {
                return Result.Fail<TicketDto>(ex.Message);
            }
        }
        else
        {
            try
            {
                order.Confirm(cmd.PaymentMethod, cmd.CashReceived);
            }
            catch (DomainException ex)
            {
                return Result.Fail<TicketDto>(ex.Message);
            }
        }

        var folioNumber = await _tickets.GetNextFolioNumberAsync(ct);
        var folio = folioNumber.ToString().PadLeft(5, '0');
        var ticket = Ticket.Generate(order, session.CashierName, _shopName, folio);

        await _orders.AddAsync(order, ct);
        await _tickets.AddAsync(ticket, ct);

        if (cmd.PaymentMethod == PaymentMethod.PayLater && customer is not null)
        {
            var debtAmount = order.Total - cmd.AdvancePayment;
            if (debtAmount > 0)
            {
                var debt = CustomerDebt.Create(
                    customer.Id,
                    customer.Name,
                    order.Id,
                    folio,
                    debtAmount,
                    cmd.DebtNote);
                await _debts.AddAsync(debt, ct);
            }
        }

        // Actualiza el efectivo en caja según el método de pago
        decimal cashInflow;
        if (order.SecondaryPaymentMethod.HasValue)
            cashInflow = order.CashReceived;  // pago mixto: solo la parte en efectivo
        else
            cashInflow = cmd.PaymentMethod switch
            {
                PaymentMethod.Cash    => order.Total,
                PaymentMethod.PayLater when cmd.AdvancePaymentMethod == PaymentMethod.Cash => cmd.AdvancePayment,
                _                    => 0m,
            };
        if (cashInflow > 0)
        {
            session.AddCash(cashInflow);
            await _sessions.SaveChangesAsync(ct);
        }

        var dto = new TicketDto(
            ticket.Id,
            ticket.Folio,
            ticket.OrderId,
            ticket.CreatedAt,
            ticket.CashierName,
            ticket.ShopName,
            order.Items.Select(i => new TicketItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.Unit,
                i.UnitPrice,
                i.LineTotal
            )).ToList(),
            ticket.Subtotal,
            ticket.DiscountAmount,
            ticket.Total,
            ticket.CashReceived,
            ticket.Change,
            ticket.PaymentMethod,
            ticket.CustomerName,
            order.SecondaryPaymentMethod,
            order.SecondaryAmount
        );

        return Result.Ok(dto);
    }
}