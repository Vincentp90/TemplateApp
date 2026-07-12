using Application;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class AddWishlistItemUseCaseTests
{
    [Fact]
    public async Task AddsItemAndPublishesEvent()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 42;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.AppIsOnWishlistAsync(USERID, APPID)).ReturnsAsync(false);

        // Capture the WishlistItem passed to AddWishlistItemAsync
        WishlistItem? capturedItem = null;
        repositoryMock
            .Setup(x => x.AddWishlistItemAsync(It.IsAny<WishlistItem>()))
            .Returns<WishlistItem>(item => { capturedItem = item; return Task.CompletedTask; });

        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var eventPublisherMock = new Mock<IEventPublisher>();
        var useCase = new AddWishlistItemUseCase(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);

        // Act
        await useCase.ExecuteAsync(new AddWishlistItemRequest(USERID, APPID));

        // Assert
        capturedItem.Should().NotBeNull();
        capturedItem!.UserId.Should().Be(USERID);
        capturedItem.AppId.Should().Be(APPID);
        capturedItem.DateAdded.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ThrowsWhenItemAlreadyOnWishlist()
    {
        // Arrange
        const int USERID = 1;
        const int APPID = 42;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.AppIsOnWishlistAsync(USERID, APPID)).ReturnsAsync(true);

        var useCase = new AddWishlistItemUseCase(
            repositoryMock.Object,
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IEventPublisher>());

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync(new AddWishlistItemRequest(USERID, APPID));
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Item already on wishlist");

        repositoryMock.Verify(x => x.AddWishlistItemAsync(It.IsAny<WishlistItem>()), Times.Never);
    }
}
