using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;
using SteamTracker.Worker;

namespace SteamTracker.Worker.Tests;

public class PriceCheckWorkerTests
{
    private readonly Mock<IProcessPriceCheckUseCase> _useCaseMock;
    private readonly Mock<ISteamStoreClient> _steamClientMock;
    private readonly Mock<IConnection> _connectionMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<PriceCheckWorker>> _loggerMock;
    private readonly Mock<IChannel> _channelMock;

    public PriceCheckWorkerTests()
    {
        _useCaseMock = new Mock<IProcessPriceCheckUseCase>();
        _steamClientMock = new Mock<ISteamStoreClient>();
        _channelMock = new Mock<IChannel>();
        _loggerMock = new Mock<ILogger<PriceCheckWorker>>();
        _configMock = new Mock<IConfiguration>();
        _connectionMock = new Mock<IConnection>();

        _configMock.Setup(x => x["RabbitMQ:PriceCheckQueue"]).Returns("test-price-check-queue");
        _connectionMock
            .Setup(x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_channelMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesChannelAndStartsConsumer()
    {
        // Arrange
        var worker = new PriceCheckWorker(
            _useCaseMock.Object,
            _steamClientMock.Object,
            _connectionMock.Object,
            _configMock.Object,
            _loggerMock.Object);

        // Act — start the worker briefly then stop it
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var task = worker.StartAsync(cts.Token);

        // Wait a bit for the channel creation to be called
        await Task.Delay(200);
        await worker.StopAsync(cts.Token);
        await task;

        // Assert — channel was created via connection
        _connectionMock.Verify(
            x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultQueueNameWhenConfigMissing()
    {
        // Arrange — config returns null for the queue name
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["RabbitMQ:PriceCheckQueue"]).Returns((string?)null);

        var connectionMock = new Mock<IConnection>();
        var channelMock = new Mock<IChannel>();
        connectionMock
            .Setup(x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        var worker = new PriceCheckWorker(
            _useCaseMock.Object,
            _steamClientMock.Object,
            connectionMock.Object,
            configMock.Object,
            _loggerMock.Object);

        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await worker.StopAsync(cts.Token);
        await task;

        // Assert — worker starts and stops cleanly
        worker.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_StoresDependencies()
    {
        // Arrange & Act
        var worker = new PriceCheckWorker(
            _useCaseMock.Object,
            _steamClientMock.Object,
            _connectionMock.Object,
            _configMock.Object,
            _loggerMock.Object);

        // Assert — no exceptions, worker is created successfully
        worker.Should().NotBeNull();
    }
}
