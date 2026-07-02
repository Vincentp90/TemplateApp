using FluentAssertions;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Tests;

public class GetWishlistWithPricesQueryTests
{
    [Fact]
    public async Task ExecuteAsync_returns_tracked_games_with_prices()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(9.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new[] { trackedGame });

        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var query = new GetWishlistWithPricesQuery(
            trackedGameRepo.Object,
            gameRepo.Object);

        var result = await query.ExecuteAsync("user-1");

        result.Should().ContainSingle();
        result[0].AppId.Should().Be(42);
        result[0].GameName.Should().Be("Test Game");
        result[0].CurrentPrice.Should().Be(9.99m);
        result[0].Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task ExecuteAsync_returns_empty_when_no_tracked_games()
    {
        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(Array.Empty<TrackedGame>());

        var query = new GetWishlistWithPricesQuery(
            trackedGameRepo.Object,
            new Mock<IGameRepository>().Object);

        var result = await query.ExecuteAsync("user-1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_skips_games_without_price_data()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new[] { trackedGame });

        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync((Game?)null);

        var query = new GetWishlistWithPricesQuery(
            trackedGameRepo.Object,
            gameRepo.Object);

        var result = await query.ExecuteAsync("user-1");

        result.Should().BeEmpty();
    }
}
