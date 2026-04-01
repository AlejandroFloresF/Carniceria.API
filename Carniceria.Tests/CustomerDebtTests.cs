using Carniceria.Domain.Common;
using Carniceria.Domain.Entities;
using Xunit;

namespace Carniceria.Tests;

public class CustomerDebtTests
{
    [Fact]
    public void Create_WithPositiveAmount_Succeeds()
    {
        var debt = CustomerDebt.Create(Guid.NewGuid(), "Juan", Guid.NewGuid(), "00001", 348m);

        Assert.Equal(348m, debt.Amount);
        Assert.Equal(DebtStatus.Pending, debt.Status);
    }

    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        Assert.Throws<DomainException>(() =>
            CustomerDebt.Create(Guid.NewGuid(), "Juan", Guid.NewGuid(), "00001", 0m));
    }

    [Fact]
    public void MarkAsPaid_Cash_RecordsMethodAndCashReceived()
    {
        var debt = CustomerDebt.Create(Guid.NewGuid(), "Juan", Guid.NewGuid(), "00001", 348m);

        debt.MarkAsPaid(PaymentMethod.Cash, 400m);

        Assert.Equal(DebtStatus.Paid, debt.Status);
        Assert.Equal(PaymentMethod.Cash, debt.PaidWithMethod);
        Assert.Equal(400m, debt.PaidCashReceived);
        Assert.NotNull(debt.PaidAt);
    }

    [Fact]
    public void MarkAsPaid_Card_RecordsCorrectMethod()
    {
        var debt = CustomerDebt.Create(Guid.NewGuid(), "Juan", Guid.NewGuid(), "00001", 200m);

        debt.MarkAsPaid(PaymentMethod.Card, 200m);

        Assert.Equal(PaymentMethod.Card, debt.PaidWithMethod);
    }

    [Fact]
    public void MarkAsPaid_AlreadyPaid_Throws()
    {
        var debt = CustomerDebt.Create(Guid.NewGuid(), "Juan", Guid.NewGuid(), "00001", 348m);
        debt.MarkAsPaid(PaymentMethod.Cash, 348m);

        Assert.Throws<DomainException>(() => debt.MarkAsPaid(PaymentMethod.Cash, 348m));
    }
}
