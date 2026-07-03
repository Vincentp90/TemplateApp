using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SteamTracker.Application.Ports;
using SteamTracker.Worker;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Worker.Tests;

public class WishlistSyncConsumerTests
{
    private readonly Mock<IHandleWishlistItemAddedUseCase> _addedUseCaseMock;
    private readonly Mock<IHandleWishlistItemRemovedUseCase> _removedUseCaseMock;
    private readonly Mock<IChannel> _channelMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly WishlistSyncConsumer _consumer;

    public WishlistSyncConsumerTests()
    {
        _addedUseCaseMock = new Mock<IHandleWishlistItemAddedUseCase>();
        _removedUseCaseMock = new Mock<IHandleWishlistItemRemovedUseCase>();
        _channelMock = new Mock<IChannel>();
        _loggerMock = new Mock<ILogger>();
        _consumer = new WishlistSyncConsumer(
            _addedUseCaseMock.Object,
            _removedUseCaseMock.Object,
            _channelMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_AddEvent_CallsAddedUseCase()
    {
        // Arrange
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = "user-1", appId = 42, addedAt = addedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _addedUseCaseMock.Verify(
            x => x.ExecuteAsync("user-1", 42, addedAt, It.IsAny<CancellationToken>()),
            Times.Once);
        _removedUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _channelMock.Verify(
            x => x.BasicAckAsync(1, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_RemoveEvent_CallsRemovedUseCase()
    {
        // Arrange
        var removedAt = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var msg = new { userId = "user-2", appId = 99, removedAt = removedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 2,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _removedUseCaseMock.Verify(
            x => x.ExecuteAsync("user-2", 99, It.IsAny<CancellationToken>()),
            Times.Once);
        _addedUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _channelMock.Verify(
            x => x.BasicAckAsync(2, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_RemoveEvent_ParsesCorrectly()
    {
        // Arrange
        var removedAt = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var msg = new { userId = "user-removed", appId = 555, removedAt = removedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 3,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _removedUseCaseMock.Verify(
            x => x.ExecuteAsync("user-removed", 555, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_AddEvent_ParsesCorrectly()
    {
        // Arrange
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = "abc-def", appId = 777, addedAt = addedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 4,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert
        _addedUseCaseMock.Verify(
            x => x.ExecuteAsync("abc-def", 777, addedAt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_BothEventTypes_DistinguishByRemovedAt()
    {
        // Arrange — message with addedAt but no removedAt → "added"
        var addedAt = DateTimeOffset.UtcNow;
        var msg = new { userId = "user", appId = 1, addedAt = addedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 5,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — should call added, not removed
        _addedUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        _removedUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_MalformedJson_StillAcks()
    {
        // Arrange — invalid JSON triggers exception → nack with requeue
        var body = Encoding.UTF8.GetBytes("{ invalid json");

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 6,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — JsonDocument.Parse throws → caught → nack with requeue
        _channelMock.Verify(
            x => x.BasicNackAsync(6, multiple: false, requeue: true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_AddEventWithMissingFields_StillAcks()
    {
        // Arrange — missing addedAt field → deserializes with default DateTimeOffset.MinValue → use case called, still acks
        var msg = new { userId = "user-1", appId = 42 };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 7,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — deserializes with default AddedAt → use case called with default, still acks
        _addedUseCaseMock.Verify(
            x => x.ExecuteAsync("user-1", 42, DateTimeOffset.MinValue, It.IsAny<CancellationToken>()),
            Times.Once);
        _channelMock.Verify(
            x => x.BasicAckAsync(7, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
