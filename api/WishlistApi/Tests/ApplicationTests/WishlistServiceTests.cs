using Application;
using Application.Commands;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Tests.ApplicationTests
{
    public class WishlistServiceTests
    {
        [Fact]
        public async Task WishlistStatsAreCorrect()
        {
            // Arrange
            const int USERID = 1;
            const string OLDESTAPPNAME = "This app is so old";

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<Domain.WishlistItem>()
                {
                    new Domain.WishlistItem(
                        id: 1,
                        appId: 1,
                        name: OLDESTAPPNAME,
                        dateAdded: DateTimeOffset.Now.AddDays(-20),
                        userId: USERID
                    ),
                    new Domain.WishlistItem(
                        id: 2,
                        appId: 2,
                        name: "A Whole Lot of aaaaaaaa",
                        dateAdded: DateTimeOffset.Now.AddDays(-13),
                        userId: USERID
                    ),
                    new Domain.WishlistItem(
                        id: 3,
                        appId: 3,
                        name: "MockAppName",
                        dateAdded: DateTimeOffset.Now.AddDays(-10),
                        userId: USERID
                    ),
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var eventPublisherMock = new Mock<IEventPublisher>();
            var wishlistService = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);

            // Act
            var result = await wishlistService.GetWishlistStatsAsync(USERID);

            // Assert
            result.Should().NotBeNull();
            result.AvgTimeAdded.TotalDays.Should().BeApproximately(14.33, 0.1); // 10+13+20 = 43 days / 3 = 14.3 days
            result.AvgTimeBetweenAdded.Days.Should().Be(5); // (3 + 7) / 2 = 5 days
            result.OldestItem.Should().Be(OLDESTAPPNAME);
            result.MostCommonCharacter.Should().Be("a");
        }

        [Fact]
        public async Task WishlistStatsCanHandleEmptyWishlist()
        {
            // Arrange
            const int USERID = 1;

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<Domain.WishlistItem>());

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var eventPublisherMock = new Mock<IEventPublisher>();
            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);

            // Act
            var result = await WishlistService.GetWishlistStatsAsync(USERID);

            // Assert
            result.Should().NotBeNull();
            result.AvgTimeAdded.Days.Should().Be(0);
            result.AvgTimeBetweenAdded.Days.Should().Be(0);
            result.OldestItem.Should().Be("");
            result.MostCommonCharacter.Should().Be("");
        }

        [Fact]
        public async Task WishlistStatsTimeBetweenZeroWithOneItem()
        {
            // Arrange
            const int USERID = 1;

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<Domain.WishlistItem>()
                {
                    new Domain.WishlistItem(
                        id: 3,
                        appId: 3,
                        name: "MockAppName",
                        dateAdded: DateTimeOffset.Now.AddDays(-10),
                        userId: USERID
                    ),
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var eventPublisherMock = new Mock<IEventPublisher>();
            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);

            // Act
            var result = await WishlistService.GetWishlistStatsAsync(USERID);

            // Assert
            result.Should().NotBeNull();
            result.AvgTimeAdded.Days.Should().Be(10);
            result.AvgTimeBetweenAdded.Days.Should().Be(0);
        }

        [Fact]
        public async Task WishlistStatsEqualCharacterCountPicksOneOfTheMostCommon()
        {
            // Arrange
            const int USERID = 1;

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
            new List<Domain.WishlistItem>()
            {
                new Domain.WishlistItem(
                    id: 1,
                    appId: 1,
                    name: "dddd",
                    dateAdded: DateTimeOffset.Now.AddDays(-14),
                    userId: USERID
                ),
                new Domain.WishlistItem(
                    id: 2,
                    appId: 2,
                    name: "bbbb",
                    dateAdded: DateTimeOffset.Now.AddDays(-13),
                    userId: USERID
                ),
                new Domain.WishlistItem(
                    id: 3,
                    appId: 3,
                    name: "ccc",
                    dateAdded: DateTimeOffset.Now.AddDays(-10),
                    userId: USERID
                ),
            });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var eventPublisherMock = new Mock<IEventPublisher>();
            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);

            // Act
            var result = await WishlistService.GetWishlistStatsAsync(USERID);

            // Assert
            result.Should().NotBeNull();
            result.MostCommonCharacter.Should().BeOneOf("d", "b");
            result.MostCommonCharacter.Should().NotBe("c");
        }

        [Fact]
        public async Task WishlistStatsMostCommonCharacterIgnoresSpaces()
        {
            // Arrange
            const int USERID = 1;

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<Domain.WishlistItem>()
                {
                    new Domain.WishlistItem(
                        id: 1,
                        appId: 1,
                        name: "a a c d",
                        dateAdded: DateTimeOffset.Now.AddDays(-14),
                        userId: USERID
                    ),
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var eventPublisherMock = new Mock<IEventPublisher>();
            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);

            // Act
            var result = await WishlistService.GetWishlistStatsAsync(USERID);

            // Assert
            result.Should().NotBeNull();
            result.MostCommonCharacter.Should().Be("a");
            result.MostCommonCharacter.Should().NotBe(" ");
        }

        [Fact]
        public async Task AddToWishlistAsyncSetsDateAddedToUtcNow()
        {
            // Arrange
            const int USERID = 1;
            const int APPID = 42;

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.AppIsOnWishlistAsync(USERID, APPID)).ReturnsAsync(false);

            // Capture the WishlistItem passed to AddWishlistItemAsync
            Domain.WishlistItem? capturedItem = null;
            repositoryMock
                .Setup(x => x.AddWishlistItemAsync(It.IsAny<Domain.WishlistItem>()))
                .Returns<Domain.WishlistItem>(item => { capturedItem = item; return Task.CompletedTask; });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var eventPublisherMock = new Mock<IEventPublisher>();
            var wishlistService = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);
            var command = new AddToWishlistCommand(USERID, APPID);

            // Act
            await wishlistService.AddToWishlistAsync(command);

            // Assert
            capturedItem.Should().NotBeNull();
            capturedItem!.DateAdded.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<object>()), Times.Once);
        }
    }
}
