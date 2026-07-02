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

        var publisher = new Mock<IPriceCheckJobPublisher>();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo.Object,
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
    public async Task ExecuteAsync_is_idempotent_when_already_tracking()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var publisher = new Mock<IPriceCheckJobPublisher>();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo.Object,
            publisher.Object);

        await useCase.ExecuteAsync("user-1", 42, DateTimeOffset.UtcNow);

        trackedGameRepo.Verify(r => r.SaveAsync(It.IsAny<TrackedGame>()), Times.Never);
        publisher.Verify(p => p.EnqueueAsync(It.IsAny<int>()), Times.Never);
    }
}
