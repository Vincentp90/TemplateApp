using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Worker;

namespace SteamTracker.Worker.Tests;

public class PriceCheckSchedulerTests
{
    private readonly Mock<ITrackedGameRepository> _trackedGameRepoMock;
    private readonly Mock<IPriceCheckJobPublisher> _publisherMock;
    private readonly Mock<ILogger<PriceCheckScheduler>> _loggerMock;
    private readonly PriceCheckScheduler _scheduler;

    public PriceCheckSchedulerTests()
    {
        _trackedGameRepoMock = new Mock<ITrackedGameRepository>();
        _publisherMock = new Mock<IPriceCheckJobPublisher>();
        _loggerMock = new Mock<ILogger<PriceCheckScheduler>>();
        _scheduler = new PriceCheckScheduler(
            _trackedGameRepoMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EnqueueAllActiveGames_EnqueuesUniqueAppIds()
    {
        // Arrange
        var game1 = TrackedGame.StartTracking(new SteamAppId(100), DateTimeOffset.UtcNow);
        var game2 = TrackedGame.StartTracking(new SteamAppId(200), DateTimeOffset.UtcNow);
        var game3 = TrackedGame.StartTracking(new SteamAppId(100), DateTimeOffset.UtcNow); // duplicate appId

        _trackedGameRepoMock
            .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackedGame> { game1, game2, game3 });

        // Act — call the private method via a wrapper
        await CallEnqueueAllActiveGames();

        // Assert — only 2 unique AppIds should be enqueued
        _publisherMock.Verify(
            x => x.EnqueueAsync(100, It.IsAny<CancellationToken>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.EnqueueAsync(200, It.IsAny<CancellationToken>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.EnqueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task EnqueueAllActiveGames_NoActiveGames_NoEnqueueCalls()
    {
        // Arrange
        _trackedGameRepoMock
            .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackedGame>());

        // Act
        await CallEnqueueAllActiveGames();

        // Assert
        _publisherMock.Verify(x => x.EnqueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueAllActiveGames_PublisherFailure_DoesNotThrow()
    {
        // Arrange
        var game = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        _trackedGameRepoMock
            .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackedGame> { game });
        _publisherMock
            .Setup(x => x.EnqueueAsync(42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection lost"));

        // Act — should not throw
        var act = async () => await CallEnqueueAllActiveGames();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnqueueAllActiveGames_MixedSuccessFailure_PublishesSuccessfulOnes()
    {
        // Arrange
        var game1 = TrackedGame.StartTracking(new SteamAppId(10), DateTimeOffset.UtcNow);
        var game2 = TrackedGame.StartTracking(new SteamAppId(20), DateTimeOffset.UtcNow);

        _trackedGameRepoMock
            .Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackedGame> { game1, game2 });

        _publisherMock
            .Setup(x => x.EnqueueAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));
        _publisherMock
            .Setup(x => x.EnqueueAsync(20, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await CallEnqueueAllActiveGames();

        // Assert — 20 should be enqueued, 10 should fail silently
        _publisherMock.Verify(x => x.EnqueueAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.EnqueueAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper to call the private method
    private async Task CallEnqueueAllActiveGames()
    {
        var method = typeof(PriceCheckScheduler).GetMethod("EnqueueAllActiveGames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method is null)
            throw new InvalidOperationException("EnqueueAllActiveGames method not found");

        await (Task?)method.Invoke(_scheduler, [CancellationToken.None])!;
    }
}
