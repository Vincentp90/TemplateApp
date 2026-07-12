using Application;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain.Repositories;
using Application.Events;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class DeleteWishlistItemUseCaseTests
{
    [Fact]
    public async Task DeletesItemAndPublishesRemovedEvent()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 42;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.DeleteWishlistItemAsync(USERID, APPID)).Returns(Task.CompletedTask);

        var eventPublisherMock = new Mock<IEventPublisher>();
        var useCase = new DeleteWishlistItemUseCase(repositoryMock.Object, eventPublisherMock.Object);

        // Act
        await useCase.ExecuteAsync(new DeleteWishlistItemRequest(USERID, APPID));

        // Assert
        repositoryMock.Verify(x => x.DeleteWishlistItemAsync(USERID, APPID), Times.Once);
        eventPublisherMock.Verify(p => p.PublishAsync(It.Is<WishlistItemRemoved>(
            e => e.UserId == USERID.ToString() && e.AppId == APPID)), Times.Once);
    }
}
