using Application;
using Application.Events;
using FluentAssertions;
using Infrastructure.Messaging;
using Moq;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Tests.ApplicationTests;

public class RabbitMqEventPublisherTests
{
    private const string ExchangeName = "wishlist.events";

    // Helper to create a mock IEventPublisher that captures published events
    private static (Mock<IEventPublisher> mock, List<object> captured) CreatePublisherMock()
    {
        var mock = new Mock<IEventPublisher>(MockBehavior.Strict);
        var captured = new List<object>();
        mock.Setup(p => p.PublishAsync(It.IsAny<object>()))
            .Callback<object>(obj => captured.Add(obj))
            .Returns(Task.CompletedTask);
        return (mock, captured);
    }

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

    #region IEventPublisher integration tests

    [Fact]
    public async Task WishlistService_AddToWishlistAsync_publishes_WishlistItemAdded()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 42;

        var (publisherMock, captured) = CreatePublisherMock();

        var repositoryMock = new Mock<Domain.Repositories.IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.AppIsOnWishlistAsync(USERID, APPID)).ReturnsAsync(false);
        Domain.WishlistItem? capturedItem = null;
        repositoryMock
            .Setup(x => x.AddWishlistItemAsync(It.IsAny<Domain.WishlistItem>()))
            .Returns<Domain.WishlistItem>(item => { capturedItem = item; return Task.CompletedTask; });

        var uowMock = new Mock<Domain.Helpers.IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var service = new Application.WishlistService(
            repositoryMock.Object,
            uowMock.Object,
            publisherMock.Object);

        var command = new Application.Commands.AddToWishlistCommand(USERID, APPID);

        // Act
        await service.AddToWishlistAsync(command);

        // Assert
        capturedItem.Should().NotBeNull();
        capturedItem!.DateAdded.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        captured.Should().ContainSingle()
            .Which.Should().BeOfType<WishlistItemAdded>();
        var addedEvent = (WishlistItemAdded)captured[0];
        addedEvent.UserId.Should().Be(USERID.ToString());
        addedEvent.AppId.Should().Be(APPID);
        addedEvent.AddedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task WishlistService_DeleteWishlistItemAsync_publishes_WishlistItemRemoved()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 99;

        var (publisherMock, captured) = CreatePublisherMock();

        var repositoryMock = new Mock<Domain.Repositories.IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.DeleteWishlistItemAsync(USERID, APPID)).Returns(Task.CompletedTask);

        var service = new Application.WishlistService(
            repositoryMock.Object,
            Mock.Of<Domain.Helpers.IUnitOfWork>(),
            publisherMock.Object);

        // Act
        await service.DeleteWishlistItemAsync(USERID, APPID);

        // Assert
        captured.Should().ContainSingle()
            .Which.Should().BeOfType<WishlistItemRemoved>();
        var removedEvent = (WishlistItemRemoved)captured[0];
        removedEvent.UserId.Should().Be(USERID.ToString());
        removedEvent.AppId.Should().Be(APPID);
    }

    [Fact]
    public async Task WishlistService_AddToWishlistAsync_duplicate_doesNotPublishEvent()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 42;

        var (publisherMock, captured) = CreatePublisherMock();

        var repositoryMock = new Mock<Domain.Repositories.IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.AppIsOnWishlistAsync(USERID, APPID)).ReturnsAsync(true);

        var service = new Application.WishlistService(
            repositoryMock.Object,
            Mock.Of<Domain.Helpers.IUnitOfWork>(),
            publisherMock.Object);

        var command = new Application.Commands.AddToWishlistCommand(USERID, APPID);

        // Act
        Func<Task> act = async () => await service.AddToWishlistAsync(command);

        // Assert
        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
        captured.Should().BeEmpty();
    }

    #endregion
}
