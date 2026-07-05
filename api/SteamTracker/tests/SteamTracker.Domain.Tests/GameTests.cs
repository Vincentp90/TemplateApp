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
}
