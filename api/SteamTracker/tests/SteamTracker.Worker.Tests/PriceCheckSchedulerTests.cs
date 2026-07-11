using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Worker;

namespace SteamTracker.Worker.Tests;

public class PriceCheckSchedulerTests
{
    private readonly Mock<IGameRepository> _gameRepoMock;
    private readonly Mock<IPriceCheckJobPublisher> _publisherMock;
    private readonly Mock<ILogger<PriceCheckScheduler>> _loggerMock;
    private readonly PriceCheckScheduler _scheduler;

    public PriceCheckSchedulerTests()
    {
        _gameRepoMock = new Mock<IGameRepository>();
        _publisherMock = new Mock<IPriceCheckJobPublisher>();
        _loggerMock = new Mock<ILogger<PriceCheckScheduler>>();
        _scheduler = new PriceCheckScheduler(
            _gameRepoMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EnqueueAllActiveGames_EnqueuesUniqueAppIds()
    {
        // Arrange
        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SteamAppId> { new SteamAppId(100), new SteamAppId(200), new SteamAppId(100) });

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
    public async Task EnqueueAllActiveGames_NoAppIdsDue_NoEnqueueCalls()
    {
        // Arrange
        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SteamAppId>());

        // Act
        await CallEnqueueAllActiveGames();

        // Assert
        _publisherMock.Verify(x => x.EnqueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnqueueAllActiveGames_PublisherFailure_DoesNotThrow()
    {
        // Arrange
        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SteamAppId> { new SteamAppId(42) });
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
        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SteamAppId> { new SteamAppId(10), new SteamAppId(20) });

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

    [Fact]
    public async Task EnqueueAllActiveGames_DeduplicatesSameAppId()
    {
        // Arrange — same AppId appears multiple times in the due list
        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SteamAppId> { new SteamAppId(42), new SteamAppId(42) });

        // Act
        await CallEnqueueAllActiveGames();

        // Assert — only one enqueue call per unique AppId
        _publisherMock.Verify(
            x => x.EnqueueAsync(42, It.IsAny<CancellationToken>()),
            Times.Once);
        _publisherMock.Verify(x => x.EnqueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueAllActiveGames_PublisherThrowsForSome_ContinuesWithOthers()
    {
        // Arrange — first publisher call throws, second succeeds
        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SteamAppId> { new SteamAppId(1), new SteamAppId(2), new SteamAppId(3) });

        var callCount = 0;
        _publisherMock
            .Setup(x => x.EnqueueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, CancellationToken>((appId, _) =>
            {
                callCount++;
                if (appId == 2)
                    throw new InvalidOperationException("RabbitMQ down");
            });

        // Act — should not throw, should still enqueue 1 and 3
        var act = async () => await CallEnqueueAllActiveGames();

        // Assert
        await act.Should().NotThrowAsync();
        _publisherMock.Verify(x => x.EnqueueAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.EnqueueAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.EnqueueAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueAllActiveGames_LargeNumberOfGames_EnqueuesAllUnique()
    {
        // Arrange — 50 unique AppIds
        var appIds = Enumerable.Range(1, 50)
            .Select(id => new SteamAppId(id))
            .ToList();

        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appIds);

        // Act
        await CallEnqueueAllActiveGames();

        // Assert — all 50 unique AppIds should be enqueued
        for (var i = 1; i <= 50; i++)
        {
            _publisherMock.Verify(x => x.EnqueueAsync(i, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task EnqueueAllActiveGames_SameAppIdMultipleTimes_EnqueuesOnce()
    {
        // Arrange — same AppId appears 100 times in the due list
        var appIds = Enumerable.Repeat(new SteamAppId(999), 100).ToList();

        _gameRepoMock
            .Setup(x => x.GetAppIdsDueForPriceCheckAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appIds);

        // Act
        await CallEnqueueAllActiveGames();

        // Assert — only one enqueue call for the duplicate AppId
        _publisherMock.Verify(
            x => x.EnqueueAsync(999, It.IsAny<CancellationToken>()),
            Times.Once);
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
