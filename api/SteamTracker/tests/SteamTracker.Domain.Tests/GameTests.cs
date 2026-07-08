using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class GameTests
{
    [Fact]
    public void ApplyPriceUpdate_sets_price_and_name()
    {
        var game = new Game(new SteamAppId(12345));
        var price = new Money(19.99m, "EUR");
        var at = DateTimeOffset.UtcNow;

        game.ApplyPriceUpdate(price, "Test Game", at);

        game.CurrentPrice.Should().Be(price);
        game.Name.Should().Be("Test Game");
        game.LastCheckedAt.Should().Be(at);
    }

    [Fact]
    public void Game_starts_with_null_price()
    {
        var game = new Game(new SteamAppId(42));
        game.CurrentPrice.Should().BeNull();
        game.LastCheckedAt.Should().BeNull();
    }

    [Fact]
    public void ApplyPriceUpdate_with_free_price()
    {
        var game = new Game(new SteamAppId(42));
        var at = DateTimeOffset.UtcNow;

        game.ApplyPriceUpdate(Money.Free, "Free Game", at);

        game.CurrentPrice.Should().Be(Money.Free);
        game.Name.Should().Be("Free Game");
    }

    [Fact]
    public void CanPriceCheck_returns_true_when_never_checked()
    {
        var game = new Game(new SteamAppId(42));

        game.CanPriceCheck(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CanPriceCheck_returns_true_when_more_than_24h_since_last_check()
    {
        var game = new Game(new SteamAppId(42));
        var now = DateTimeOffset.UtcNow;
        game.ApplyPriceUpdate(new Money(1000, "USD"), "Test", now);

        game.CanPriceCheck(now.AddHours(25)).Should().BeTrue();
    }

    [Fact]
    public void CanPriceCheck_returns_false_within_24h()
    {
        var game = new Game(new SteamAppId(42));
        var now = DateTimeOffset.UtcNow;
        game.ApplyPriceUpdate(new Money(1000, "USD"), "Test", now);

        game.CanPriceCheck(now.AddHours(12)).Should().BeFalse();
    }

    [Fact]
    public void CanPriceCheck_returns_true_at_exactly_24h()
    {
        var game = new Game(new SteamAppId(42));
        var now = DateTimeOffset.UtcNow;
        game.ApplyPriceUpdate(new Money(1000, "USD"), "Test", now);

        game.CanPriceCheck(now.AddHours(24)).Should().BeTrue();
    }
}
