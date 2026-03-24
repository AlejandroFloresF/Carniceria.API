using Carniceria.Domain.Common;
namespace Carniceria.Domain.Entities;
public class Ticket : BaseEntity
{
    public string Folio { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public Guid CashierSessionId { get; private set; }
    public string CashierName { get; private set; } = string.Empty;
    public string ShopName { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public decimal CashReceived { get; private set; }
    public decimal Change { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? CustomerName { get; private set; }
    private Ticket() { }
    public static Ticket Generate(Order order, string cashierName, string shopName, string folio) =>
       new()
       {
           Folio = folio,
           OrderId = order.Id,
           CashierSessionId = order.CashierSessionId,
           CashierName = cashierName,
           CustomerName = order.CustomerName,
           ShopName = shopName,
           Subtotal = order.Subtotal,
           DiscountAmount = order.DiscountAmount,
           TaxAmount = order.TaxAmount,
           Total = order.Total,
           CashReceived = order.CashReceived,
           Change = order.PaymentMethod == PaymentMethod.Cash
               ? Math.Round(order.CashReceived - order.Total, 2) : 0,
           PaymentMethod = order.PaymentMethod,
       };
}
