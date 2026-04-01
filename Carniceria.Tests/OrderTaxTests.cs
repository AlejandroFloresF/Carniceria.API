using Carniceria.Domain.Entities;
using Xunit;

namespace Carniceria.Tests;

/// <summary>
/// IVA incluido en precio: TaxAmount = Total * 16/116
/// </summary>
public class OrderTaxTests
{
    private static Product MakeProduct(decimal price) =>
        Product.Create("Test", "General", price, "kg", 100m);

    [Fact]
    public void Total_IsSubtotalMinusDiscount_IvaNotAddedOnTop()
    {
        var session = CashierSession.Open("Juan", 0m);
        var order = Order.Create(session.Id);
        order.AddItem(MakeProduct(116m), 1m); // $116 con IVA incluido

        Assert.Equal(116m, order.Subtotal);
        Assert.Equal(116m, order.Total);
    }

    [Fact]
    public void TaxAmount_ExtractedFrom_Total()
    {
        var session = CashierSession.Open("Juan", 0m);
        var order = Order.Create(session.Id);
        order.AddItem(MakeProduct(116m), 1m); // $116 IVA incluido → IVA = 116*16/116 = 16

        Assert.Equal(16m, order.TaxAmount);
    }

    [Fact]
    public void TaxAmount_WithDiscount_UsesTotalAfterDiscount()
    {
        // $200 with 10% discount → Total = 180 → IVA = 180*16/116 ≈ 24.83
        var session = CashierSession.Open("Juan", 0m);
        var order = Order.Create(session.Id);
        order.AddItem(MakeProduct(200m), 1m);
        order.ApplyDiscount(10m);

        var expectedTax = Math.Round(180m * 16m / 116m, 2);
        Assert.Equal(180m, order.Total);
        Assert.Equal(expectedTax, order.TaxAmount);
    }

    [Fact]
    public void Total_DoesNotIncludeTaxOnTop()
    {
        // Old behavior added tax on top: Total = Subtotal - Discount + TaxAmount
        // New behavior: Total = Subtotal - Discount (IVA included)
        var session = CashierSession.Open("Juan", 0m);
        var order = Order.Create(session.Id);
        order.AddItem(MakeProduct(100m), 1m);

        // Total should be 100, NOT 116
        Assert.Equal(100m, order.Total);
        Assert.True(order.Total < order.Subtotal + order.TaxAmount,
            "Total should not be Subtotal + Tax (IVA is already included in price).");
    }

    [Fact]
    public void TaxAmount_MultipleItems_BasedOnTotal()
    {
        var session = CashierSession.Open("Juan", 0m);
        var order = Order.Create(session.Id);
        order.AddItem(MakeProduct(58m), 2m); // subtotal = 116

        var expectedTax = Math.Round(116m * 16m / 116m, 2); // = 16
        Assert.Equal(16m, expectedTax);
        Assert.Equal(expectedTax, order.TaxAmount);
    }
}
