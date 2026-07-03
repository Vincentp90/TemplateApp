using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.External;
using SteamTracker.Worker;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Worker.Tests;

public class PriceCheckConsumerTests
{
    private readonly Mock<IProcessPriceCheckUseCase> _useCaseMock;
    private readonly Mock<ISteamStoreClient> _steamClientMock;
    private readonly Mock<IChannel> _channelMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly PriceCheckConsumer _consumer;

    public PriceCheckConsumerTests()
    {
        _useCaseMock = new Mock<IProcessPriceCheckUseCase>();
        _steamClientMock = new Mock<ISteamStoreClient>();
        _channelMock = new Mock<IChannel>();
        _loggerMock = new Mock<ILogger>();
        _consumer = new PriceCheckConsumer(
            _useCaseMock.Object,
            _steamClientMock.Object,
            _channelMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_SuccessfulPriceFetch_AcksAndProcesses()
    {
        // Arrange
        const int appId = 12345;
        const ulong deliveryTag = 1;
        var price = new Money(19.99m, "EUR");
        var gameName = "Test Game";
        var message = new PriceCheckMessage(appId, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId))
            .ReturnsAsync((price, gameName));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _useCaseMock.Verify(
            x => x.ExecuteAsync(appId, price, gameName, It.IsAny<CancellationToken>()),
            Times.Once);
        _channelMock.Verify(
            x => x.BasicAckAsync(deliveryTag, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_NullSteamResult_NacksAndRequeues()
    {
        // Arrange
        const int appId = 99999;
        const ulong deliveryTag = 2;
        var message = new PriceCheckMessage(appId, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId))
            .ReturnsAsync((ValueTuple<Money, string>?)null);

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _useCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<int>(), It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _channelMock.Verify(
            x => x.BasicNackAsync(deliveryTag, multiple: false, requeue: true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_SteamRateLimit_NacksAndRequeues()
    {
        // Arrange
        const int appId = 54321;
        const ulong deliveryTag = 3;
        var message = new PriceCheckMessage(appId, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId))
            .ThrowsAsync(new SteamRateLimitException("Rate limit exceeded"));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _useCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<int>(), It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _channelMock.Verify(
            x => x.BasicNackAsync(deliveryTag, multiple: false, requeue: true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_GenericException_NacksAndRequeues()
    {
        // Arrange
        const int appId = 11111;
        const ulong deliveryTag = 4;
        var message = new PriceCheckMessage(appId, DateTimeOffset.UtcNow);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId))
            .ThrowsAsync(new InvalidOperationException("Steam API timeout"));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _useCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<int>(), It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _channelMock.Verify(
            x => x.BasicNackAsync(deliveryTag, multiple: false, requeue: true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_MultipleDeliveries_AllProcessed()
    {
        // Arrange
        const int appId1 = 100;
        const int appId2 = 200;
        var price = new Money(9.99m, "EUR");
        var gameName = "Game";

        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId1))
            .ReturnsAsync((price, gameName));
        _steamClientMock
            .Setup(x => x.FetchPriceAsync(appId2))
            .ReturnsAsync((price, gameName));

        var msg1 = new PriceCheckMessage(appId1, DateTimeOffset.UtcNow);
        var msg2 = new PriceCheckMessage(appId2, DateTimeOffset.UtcNow);
        var body1 = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg1));
        var body2 = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg2));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag", deliveryTag: 1, redelivered: false, exchange: "", routingKey: "",
            properties: new BasicProperties(), body: body1, CancellationToken.None);

        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag", deliveryTag: 2, redelivered: false, exchange: "", routingKey: "",
            properties: new BasicProperties(), body: body2, CancellationToken.None);

        // Assert
        _useCaseMock.Verify(
            x => x.ExecuteAsync(appId1, price, gameName, It.IsAny<CancellationToken>()),
            Times.Once);
        _useCaseMock.Verify(
            x => x.ExecuteAsync(appId2, price, gameName, It.IsAny<CancellationToken>()),
            Times.Once);
        _channelMock.Verify(x => x.BasicAckAsync(It.IsAny<ulong>(), multiple: false, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_InvalidJson_LogsErrorAndNacks()
    {
        // Arrange
        const ulong deliveryTag = 5;
        var body = Encoding.UTF8.GetBytes("not valid json {{{");

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — deserialize throws JsonException → caught by generic handler → nack with requeue
        _useCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<int>(), It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _channelMock.Verify(
            x => x.BasicNackAsync(deliveryTag, multiple: false, requeue: true, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
