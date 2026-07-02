using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class AlertRuleTests
{
    [Fact]
    public void ShouldTrigger_returns_true_when_price_below_threshold()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        var price = new Money(5m);

        rule.ShouldTrigger(price).Should().BeTrue();
    }

    [Fact]
    public void ShouldTrigger_returns_true_when_price_equal_to_threshold()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        var price = new Money(10m);

        rule.ShouldTrigger(price).Should().BeTrue();
    }

    [Fact]
    public void ShouldTrigger_returns_false_when_price_above_threshold()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        var price = new Money(15m);

        rule.ShouldTrigger(price).Should().BeFalse();
    }

    [Fact]
    public void ShouldTrigger_returns_false_for_free_price()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));

        rule.ShouldTrigger(Money.Free).Should().BeFalse();
    }

    [Fact]
    public void MarkTriggered_sets_last_triggered_at()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        var at = DateTimeOffset.UtcNow;

        rule.MarkTriggered(at);

        rule.LastTriggeredAt.Should().Be(at);
    }

    [Fact]
    public void Deactivate_sets_is_active_to_false()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        rule.IsActive.Should().BeTrue();

        rule.Deactivate();

        rule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_defaults_to_true()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void LastTriggeredAt_defaults_to_null()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-id", new SteamAppId(42), new Money(10m));
        rule.LastTriggeredAt.Should().BeNull();
    }
}
