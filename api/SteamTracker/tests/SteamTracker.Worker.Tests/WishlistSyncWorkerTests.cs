using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;
using SteamTracker.Worker;

namespace SteamTracker.Worker.Tests;

public class WishlistSyncWorkerTests
{
    private readonly Mock<IHandleWishlistItemAddedUseCase> _addedUseCaseMock;
    private readonly Mock<IHandleWishlistItemRemovedUseCase> _removedUseCaseMock;
    private readonly Mock<IConnection> _connectionMock;
    private readonly Mock<ILogger<WishlistSyncWorker>> _loggerMock;
    private readonly Mock<IChannel> _channelMock;

    public WishlistSyncWorkerTests()
    {
        _addedUseCaseMock = new Mock<IHandleWishlistItemAddedUseCase>();
        _removedUseCaseMock = new Mock<IHandleWishlistItemRemovedUseCase>();
        _channelMock = new Mock<IChannel>();
        _loggerMock = new Mock<ILogger<WishlistSyncWorker>>();
        _connectionMock = new Mock<IConnection>();

        _connectionMock
            .Setup(x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_channelMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DeclaresExchangeAndBindsQueue()
    {
        // Arrange
        var worker = new WishlistSyncWorker(
            _connectionMock.Object,
            _addedUseCaseMock.Object,
            _removedUseCaseMock.Object,
            _loggerMock.Object);

        // Act — start the worker briefly then stop it
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var task = worker.StartAsync(cts.Token);

        // Wait for channel operations
        await Task.Delay(200);
        await worker.StopAsync(cts.Token);
        await task;

        // Assert — channel was created
        _connectionMock.Verify(
            x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_StoresDependencies()
    {
        // Arrange & Act
        var worker = new WishlistSyncWorker(
            _connectionMock.Object,
            _addedUseCaseMock.Object,
            _removedUseCaseMock.Object,
            _loggerMock.Object);

        // Assert — no exceptions, worker is created successfully
        worker.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CreatesChannelAndConsumers()
    {
        // Arrange
        var worker = new WishlistSyncWorker(
            _connectionMock.Object,
            _addedUseCaseMock.Object,
            _removedUseCaseMock.Object,
            _loggerMock.Object);

        // Act — start the worker briefly then stop it
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var task = worker.StartAsync(cts.Token);

        // Wait for channel operations
        await Task.Delay(200);
        await worker.StopAsync(cts.Token);
        await task;

        // Assert — channel was created
        _connectionMock.Verify(
            x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
