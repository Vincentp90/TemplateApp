using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Events;
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
    public void ApplyPriceUpdate_raises_PriceUpdatedEvent()
    {
        var game = new Game(new SteamAppId(12345));
        var price = new Money(9.99m);
        var at = DateTimeOffset.UtcNow;

        game.ApplyPriceUpdate(price, "Test Game", at);

        game.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PriceUpdatedEvent>();
    }

    [Fact]
    public void ApplyPriceUpdate_updates_old_price()
    {
        var game = new Game(new SteamAppId(12345));
        var price1 = new Money(10m);
        game.ApplyPriceUpdate(price1, "Game", DateTimeOffset.UtcNow);

        var price2 = new Money(5m);
        game.ApplyPriceUpdate(price2, "Game Updated", DateTimeOffset.UtcNow);

        game.DomainEvents.Should().HaveCount(2);
        var first = (PriceUpdatedEvent)game.DomainEvents[0];
        first.OldPrice.Should().BeNull();
        first.NewPrice.Should().Be(price1);

        var second = (PriceUpdatedEvent)game.DomainEvents[1];
        second.OldPrice.Should().Be(price1);
        second.NewPrice.Should().Be(price2);
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
}
