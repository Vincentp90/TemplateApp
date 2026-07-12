using Application;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Application.Events;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class PublishBackfillEventUseCaseTests
{
    [Fact]
    public async Task PublishesWishlistItemAddedEvent()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 42;
        var dateAdded = DateTimeOffset.UtcNow.AddDays(-5);

        var eventPublisherMock = new Mock<IEventPublisher>();
        var useCase = new PublishBackfillEventUseCase(eventPublisherMock.Object);

        // Act
        await useCase.ExecuteAsync(new PublishBackfillEventRequest(USERID, APPID, dateAdded));

        // Assert
        eventPublisherMock.Verify(p => p.PublishAsync(It.Is<WishlistItemAdded>(
            e => e.UserId == USERID.ToString() && e.AppId == APPID && e.AddedAt == dateAdded)), Times.Once);
    }
}
