using Application.Events;
using FluentAssertions;
using System.Text.Json;

namespace Tests.ApplicationTests;

public class RabbitMqEventPublisherTests
{
    #region Event record serialization tests

    [Fact]
    public void WishlistItemAdded_SerializesCorrectly()
    {
        // Arrange
        var @event = new WishlistItemAdded("user-1", 123, new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Act
        var json = JsonSerializer.Serialize(@event, jsonOptions);

        // Assert
        var deserialized = JsonSerializer.Deserialize<WishlistItemAdded>(json, jsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-1");
        deserialized.AppId.Should().Be(123);
        deserialized.AddedAt.Should().Be(new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void WishlistItemRemoved_SerializesCorrectly()
    {
        // Arrange
        var @event = new WishlistItemRemoved("user-2", 456, new DateTimeOffset(2024, 6, 20, 14, 0, 0, TimeSpan.Zero));
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Act
        var json = JsonSerializer.Serialize(@event, jsonOptions);

        // Assert
        var deserialized = JsonSerializer.Deserialize<WishlistItemRemoved>(json, jsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user-2");
        deserialized.AppId.Should().Be(456);
        deserialized.RemovedAt.Should().Be(new DateTimeOffset(2024, 6, 20, 14, 0, 0, TimeSpan.Zero));
    }

    #endregion
}
