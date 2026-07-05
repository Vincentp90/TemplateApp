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
    public async Task HandleBasicDeliverAsync_MalformedJson_DeadLetters()
    {
        // Arrange — invalid JSON triggers exception → dead-letter
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

        // Assert — JsonDocument.Parse throws → caught → dead-letter (requeue: false)
        _channelMock.Verify(
            x => x.BasicNackAsync(6, multiple: false, requeue: false, It.IsAny<CancellationToken>()),
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

    [Fact]
    public async Task HandleBasicDeliverAsync_AddUseCaseThrows_DeadLetters()
    {
        // Arrange
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = "user-throw", appId = 123, addedAt = addedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        _addedUseCaseMock
            .Setup(x => x.ExecuteAsync("user-throw", 123, addedAt, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Use case failed"));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 8,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — programming error → dead-letter (requeue: false)
        _channelMock.Verify(
            x => x.BasicNackAsync(8, multiple: false, requeue: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_RemoveUseCaseThrows_DeadLetters()
    {
        // Arrange
        var removedAt = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var msg = new { userId = "user-throw-remove", appId = 456, removedAt = removedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        _removedUseCaseMock
            .Setup(x => x.ExecuteAsync("user-throw-remove", 456, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Remove use case failed"));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 9,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — programming error → dead-letter (requeue: false)
        _channelMock.Verify(
            x => x.BasicNackAsync(9, multiple: false, requeue: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_OnlyRemovedAtField_DetectsRemoveEvent()
    {
        // Arrange — message has only removedAt, no addedAt → should be detected as remove
        var removedAt = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var msg = new { userId = "only-removed", appId = 999, removedAt = removedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 10,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — should call removed use case
        _removedUseCaseMock.Verify(
            x => x.ExecuteAsync("only-removed", 999, It.IsAny<CancellationToken>()),
            Times.Once);
        _addedUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_BothAddedAndRemovedFields_TreatsAsRemove()
    {
        // Arrange — when both fields exist, removedAt takes precedence (remove event)
        var removedAt = new DateTimeOffset(2025, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var addedAt = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var msg = new { userId = "both-fields", appId = 777, addedAt = addedAt, removedAt = removedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 11,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — removedAt takes precedence → removed use case called
        _removedUseCaseMock.Verify(
            x => x.ExecuteAsync("both-fields", 777, It.IsAny<CancellationToken>()),
            Times.Once);
        _addedUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_AddWithNullUserId_Acks()
    {
        // Arrange — null userId still deserializes and calls use case
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = (string?)null, appId = 42, addedAt = addedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 12,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — null userId is passed through to use case
        _addedUseCaseMock.Verify(
            x => x.ExecuteAsync((string?)null, 42, addedAt, It.IsAny<CancellationToken>()),
            Times.Once);
        _channelMock.Verify(
            x => x.BasicAckAsync(12, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_AddWithNegativeAppId_Acks()
    {
        // Arrange — negative AppId still deserializes and calls use case
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = "user", appId = -1, addedAt = addedAt };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 13,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — negative AppId is passed through to use case
        _addedUseCaseMock.Verify(
            x => x.ExecuteAsync("user", -1, addedAt, It.IsAny<CancellationToken>()),
            Times.Once);
        _channelMock.Verify(
            x => x.BasicAckAsync(13, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBasicDeliverAsync_JsonWithExtraFields_DoesNotThrow()
    {
        // Arrange — extra fields in JSON should not cause issues
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = "user", appId = 42, addedAt = addedAt, extraField = "ignored", anotherExtra = 123 };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // Act
        await _consumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 14,
            redelivered: false,
            exchange: "wishlist.events",
            routingKey: "",
            properties: new BasicProperties(),
            body: body,
            CancellationToken.None);

        // Assert — extra fields are ignored, use case still called
        _addedUseCaseMock.Verify(
            x => x.ExecuteAsync("user", 42, addedAt, It.IsAny<CancellationToken>()),
            Times.Once);
        _channelMock.Verify(
            x => x.BasicAckAsync(14, multiple: false, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
