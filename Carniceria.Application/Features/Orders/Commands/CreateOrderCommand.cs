using Carniceria.Application.Common;
using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Carniceria.Domain.Interfaces;
using MediatR;

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
    PaymentMethod? AdvancePaymentMethod = null
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

    public CreateOrderHandler(
        IOrderRepository orders,
        IProductRepository products,
        ITicketRepository tickets,
        ISessionRepository sessions,
        ICustomerRepository customers,
        ICustomerDebtRepository debts,
        ICustomerProductPriceRepository prices)
    {
        _orders = orders;
        _products = products;
        _tickets = tickets;
        _sessions = sessions;
        _customers = customers;
        _debts = debts;
        _prices = prices;
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
                return Result.Fail<TicketDto>($"Insufficient stock for {product.Name}.");

            // Si el cliente tiene precio especial para este producto, lo aplica
            if (customPriceMap.TryGetValue(product.Id, out var customPrice))
                order.AddItemWithCustomPrice(product, item.Quantity, customPrice);
            else
                order.AddItem(product, item.Quantity);

            product.DeductStock(item.Quantity);
        }

        // ── 4. Aplica descuento y asigna cliente ─────────────
        if (customer is not null)
        {
            order.AssignCustomer(customer);
            // Si tiene precios por producto, el descuento % no se apila
            // Si no tiene precios especiales, aplica su descuento % general
            if (customPriceMap.Count == 0 && customer.DiscountPercent > 0)
                order.ApplyDiscount(customer.DiscountPercent);
        }
        else if (cmd.DiscountPercent > 0)
        {
            order.ApplyDiscount(cmd.DiscountPercent);
        }

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
        var ticket = Ticket.Generate(order, session.CashierName, "Carnicería La Única", folio);

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
            ticket.TaxAmount,
            ticket.Total,
            ticket.CashReceived,
            ticket.Change,
            ticket.PaymentMethod,
            ticket.CustomerName
        );

        return Result.Ok(dto);
    }
}