using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Xunit;

namespace Carniceria.Tests;

public class CashierSessionTests
{
    // ── Open ──────────────────────────────────────────────────────────────
    [Fact]
    public void Open_SetsCurrentCashToOpeningCash()
    {
        var session = CashierSession.Open("Juan", 500m);

        Assert.Equal(500m, session.OpeningCash);
        Assert.Equal(500m, session.CurrentCash);
        Assert.Equal(SessionStatus.Open, session.Status);
    }

    [Fact]
    public void Open_WithZeroOpeningCash_IsValid()
    {
        var session = CashierSession.Open("Ana", 0m);

        Assert.Equal(0m, session.CurrentCash);
    }

    [Fact]
    public void Open_WithNegativeOpeningCash_Throws()
    {
        Assert.Throws<DomainException>(() => CashierSession.Open("Juan", -1m));
    }

    [Fact]
    public void Open_WithEmptyCashierName_Throws()
    {
        Assert.Throws<DomainException>(() => CashierSession.Open("", 100m));
    }

    // ── AddCash ───────────────────────────────────────────────────────────
    [Fact]
    public void AddCash_IncreasesCurrentCash()
    {
        var session = CashierSession.Open("Juan", 500m);

        session.AddCash(300m);

        Assert.Equal(800m, session.CurrentCash);
    }

    [Fact]
    public void AddCash_MultipleTimes_AccumulatesCorrectly()
    {
        var session = CashierSession.Open("Juan", 500m);

        session.AddCash(100m);
        session.AddCash(250m);
        session.AddCash(50m);

        Assert.Equal(900m, session.CurrentCash);
    }

    [Fact]
    public void AddCash_WithZero_DoesNotChange()
    {
        var session = CashierSession.Open("Juan", 500m);

        session.AddCash(0m);

        Assert.Equal(500m, session.CurrentCash);
    }

    [Fact]
    public void AddCash_WithNegative_Throws()
    {
        var session = CashierSession.Open("Juan", 500m);

        Assert.Throws<DomainException>(() => session.AddCash(-10m));
    }

    // ── Close ─────────────────────────────────────────────────────────────
    [Fact]
    public void Close_SetsStatusClosed()
    {
        var session = CashierSession.Open("Juan", 500m);
        session.AddCash(300m);

        session.Close(800m);

        Assert.Equal(SessionStatus.Closed, session.Status);
        Assert.Equal(800m, session.ClosingCash);
        Assert.NotNull(session.ClosedAt);
    }

    [Fact]
    public void Close_AlreadyClosed_Throws()
    {
        var session = CashierSession.Open("Juan", 500m);
        session.Close(500m);

        Assert.Throws<DomainException>(() => session.Close(500m));
    }

    [Fact]
    public void CurrentCash_AfterSaleAndDebtPayment_ReflectsAllInflows()
    {
        // Scenario: open 500, sell 347.77 cash, receive 200 debt cash payment
        var session = CashierSession.Open("Juan", 500m);

        session.AddCash(347.77m); // cash sale
        session.AddCash(200m);    // debt payment in cash

        Assert.Equal(1047.77m, session.CurrentCash);
    }

    [Fact]
    public void CurrentCash_CardAndTransferPayments_NotAdded()
    {
        // Card/transfer don't go into the physical cash drawer
        var session = CashierSession.Open("Juan", 500m);

        // Only cash sale of 200 is added; card sale of 300 is NOT
        session.AddCash(200m);

        Assert.Equal(700m, session.CurrentCash);
    }
}
