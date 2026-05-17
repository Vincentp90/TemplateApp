using Application;
using DataAccess.AppListings;
using DataAccess.Users;
using DataAccess.Wishlist;
using Domain.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using WishlistApi.Controllers;

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
                new List<WishlistItem>()
                {
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-20),
                        UserID = USERID,
                        ID = 1,
                        appid = 1,
                        AppListing = new AppListing(){ appid = 1, name = OLDESTAPPNAME }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-13),
                        UserID = USERID,
                        ID = 2,
                        appid = 2,
                        AppListing = new AppListing(){ appid = 2, name = "A Whole Lot of aaaaaaaa" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-10),
                        UserID = USERID,
                        ID = 3,
                        appid = 3,
                        AppListing = new AppListing(){ appid = 3, name = "MockAppName" }
                    },
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var wishlistService = new WishlistService(repositoryMock.Object, uowMock.Object);

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
                new List<WishlistItem>());

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object);

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
                new List<WishlistItem>()
                {
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-10),
                        UserID = USERID,
                        ID = 3,
                        appid = 3,
                        AppListing = new AppListing(){ appid = 3, name = "MockAppName" }
                    },
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object);

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
                new List<WishlistItem>()
                {
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-14),
                        UserID = USERID,
                        ID = 1,
                        appid = 1,
                        AppListing = new AppListing(){ appid = 1, name = "dddd" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-13),
                        UserID = USERID,
                        ID = 2,
                        appid = 2,
                        AppListing = new AppListing(){ appid = 2, name = "bbbb" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-10),
                        UserID = USERID,
                        ID = 3,
                        appid = 3,
                        AppListing = new AppListing(){ appid = 2, name = "ccc" }
                    },
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object);

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
                new List<WishlistItem>()
                {
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-14),
                        UserID = USERID,
                        ID = 1,
                        appid = 1,
                        AppListing = new AppListing(){ appid = 1, name = "a a c d" }
                    },
                });

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var WishlistService = new WishlistService(repositoryMock.Object, uowMock.Object);

            // Act
            var result = await WishlistService.GetWishlistStatsAsync(USERID);

            // Assert
            result.Should().NotBeNull();
            result.MostCommonCharacter.Should().Be("a");
            result.MostCommonCharacter.Should().NotBe(" ");
        }
    }
}
