using FluentAssertions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Free_returns_zero_amount()
    {
        var free = Money.Free;
        free.Amount.Should().Be(0m);
        free.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Free_is_free_flagged()
    {
        Money.Free.IsFree.Should().BeTrue();
    }

    [Fact]
    public void Constructor_sets_amount_and_currency()
    {
        var money = new Money(19.99m, "USD");
        money.Amount.Should().Be(19.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Default_currency_is_eur()
    {
        var money = new Money(9.99m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Free_is_not_equal_to_regular_money()
    {
        Money.Free.Should().NotBe(new Money(0m));
    }

    [Fact]
    public void Free_is_equal_to_itself()
    {
        Money.Free.Should().Be(Money.Free);
    }

    [Fact]
    public void Two_same_amounts_are_equal()
    {
        var a = new Money(19.99m, "EUR");
        var b = new Money(19.99m, "EUR");
        a.Should().Be(b);
    }

    [Fact]
    public void Less_than_works()
    {
        var cheap = new Money(5m);
        var expensive = new Money(10m);
        (cheap < expensive).Should().BeTrue();
        (expensive < cheap).Should().BeFalse();
    }

    [Fact]
    public void Less_than_says_free_is_less_than_paid()
    {
        (Money.Free < new Money(1m)).Should().BeTrue();
    }

    [Fact]
    public void Greater_than_works()
    {
        var expensive = new Money(10m);
        var cheap = new Money(5m);
        (expensive > cheap).Should().BeTrue();
    }

    [Fact]
    public void Greater_than_says_paid_is_greater_than_free()
    {
        (new Money(1m) > Money.Free).Should().BeTrue();
    }

    [Fact]
    public void Equals_with_different_currency_returns_false()
    {
        var a = new Money(10m, "EUR");
        var b = new Money(10m, "USD");
        a.Should().NotBe(b);
    }
}
