using FluentAssertions;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Tests;

public class HandleWishlistItemAddedUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_creates_tracked_game_and_enqueues_job()
    {
        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync((TrackedGame?)null);

        var gameRepo = new Mock<IGameRepository>();
        var publisher = new Mock<IPriceCheckJobPublisher>();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo.Object,
            gameRepo.Object,
            publisher.Object);

        var addedAt = DateTimeOffset.UtcNow;
        await useCase.ExecuteAsync("user-1", 42, addedAt);

        trackedGameRepo.Verify(r => r.SaveAsync(It.Is<TrackedGame>(g =>
            g.AppId.Value == 42 &&
            g.IsActive &&
            g.TrackedSince == addedAt)), Times.Once);

        publisher.Verify(p => p.EnqueueAsync(42), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_is_idempotent_when_already_tracking_and_within_24h()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(1000, "USD"), "Test Game", DateTimeOffset.UtcNow.AddHours(-12));

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var publisher = new Mock<IPriceCheckJobPublisher>();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo.Object,
            gameRepo.Object,
            publisher.Object);

        await useCase.ExecuteAsync("user-1", 42, DateTimeOffset.UtcNow);

        trackedGameRepo.Verify(r => r.SaveAsync(It.IsAny<TrackedGame>()), Times.Never);
        publisher.Verify(p => p.EnqueueAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_enqueues_when_already_tracking_but_due_for_check()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(1000, "USD"), "Test Game", DateTimeOffset.UtcNow.AddDays(-2)); // 2 days ago

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var publisher = new Mock<IPriceCheckJobPublisher>();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo.Object,
            gameRepo.Object,
            publisher.Object);

        await useCase.ExecuteAsync("user-1", 42, DateTimeOffset.UtcNow);

        trackedGameRepo.Verify(r => r.SaveAsync(It.IsAny<TrackedGame>()), Times.Never);
        publisher.Verify(p => p.EnqueueAsync(42), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_enqueues_when_game_never_checked()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var game = new Game(new SteamAppId(42)); // LastCheckedAt is null

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var publisher = new Mock<IPriceCheckJobPublisher>();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo.Object,
            gameRepo.Object,
            publisher.Object);

        await useCase.ExecuteAsync("user-1", 42, DateTimeOffset.UtcNow);

        trackedGameRepo.Verify(r => r.SaveAsync(It.IsAny<TrackedGame>()), Times.Never);
        publisher.Verify(p => p.EnqueueAsync(42), Times.Once);
    }
}
