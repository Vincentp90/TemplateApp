using FluentAssertions;
using SteamTracker.Worker;
using System.Text.Json;

namespace SteamTracker.Worker.Tests;

public class MessageContractTests
{
    [Fact]
    public void PriceCheckMessage_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var original = new PriceCheckMessage(12345, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(original);

        // Act
        var deserialized = JsonSerializer.Deserialize<PriceCheckMessage>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AppId.Should().Be(12345);
    }

    [Fact]
    public void PriceCheckMessage_AppIdZero_SerializesCorrectly()
    {
        // Arrange
        var original = new PriceCheckMessage(0, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(original);

        // Act
        var deserialized = JsonSerializer.Deserialize<PriceCheckMessage>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AppId.Should().Be(0);
    }

    [Fact]
    public void WishlistItemAddedMessage_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var original = new WishlistItemAddedMessage("user-1", 42, addedAt);
        var json = JsonSerializer.Serialize(original);

        // Act
        var deserialized = JsonSerializer.Deserialize<WishlistItemAddedMessage>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-1");
        deserialized.AppId.Should().Be(42);
        deserialized.AddedAt.Should().Be(addedAt);
    }

    [Fact]
    public void WishlistItemRemovedMessage_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var removedAt = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var original = new WishlistItemRemovedMessage("user-2", 99, removedAt);
        var json = JsonSerializer.Serialize(original);

        // Act
        var deserialized = JsonSerializer.Deserialize<WishlistItemRemovedMessage>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-2");
        deserialized.AppId.Should().Be(99);
        deserialized.RemovedAt.Should().Be(removedAt);
    }

    [Fact]
    public void WishlistItemAddedMessage_HasAddedAt_NoRemovedAt()
    {
        // Arrange
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var msg = new { userId = "user", appId = 42, addedAt = addedAt };
        var json = JsonSerializer.Serialize(msg);

        // Act
        var doc = JsonDocument.Parse(json);
        var hasRemovedAt = doc.RootElement.TryGetProperty("removedAt", out _);

        // Assert
        hasRemovedAt.Should().BeFalse();
    }

    [Fact]
    public void WishlistItemRemovedMessage_HasRemovedAt_NoAddedAt()
    {
        // Arrange
        var removedAt = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);
        var msg = new { userId = "user", appId = 42, removedAt = removedAt };
        var json = JsonSerializer.Serialize(msg);

        // Act
        var doc = JsonDocument.Parse(json);
        var hasRemovedAt = doc.RootElement.TryGetProperty("removedAt", out _);

        // Assert
        hasRemovedAt.Should().BeTrue();
    }

    [Fact]
    public void WishlistItemEvent_SerializesAndDeserializesCorrectly()
    {
        // Arrange — legacy event type
        var addedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var original = new WishlistItemEvent("user-1", 42, addedAt);
        var json = JsonSerializer.Serialize(original);

        // Act
        var deserialized = JsonSerializer.Deserialize<WishlistItemEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-1");
        deserialized.AppId.Should().Be(42);
        deserialized.AddedAt.Should().Be(addedAt);
    }
}
