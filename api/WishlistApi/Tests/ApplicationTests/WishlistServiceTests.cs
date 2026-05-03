using DataAccess.AppListings;
using DataAccess.Users;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using WishlistApi.Controllers;
using Application.Wishlist;
using FluentAssertions;

namespace Tests.ApplicationTests
{
    public class WishlistServiceTests
    {
        [Fact]
        public void WishlistStatsAreCorrect()
        {
            // Arrange
            const int USERID = 1;
            const string OLDESTAPPNAME = "This app is so old";

            var wlDAMock = new Mock<IWishlistItemDA>(MockBehavior.Strict);
            wlDAMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<WishlistItem>()
                {
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-10),
                        UserID = USERID,
                        ID = 1,
                        appid = 1,
                        AppListing = new AppListing(){ appid = 1, name = "MockAppName" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-13),
                        UserID = USERID,
                        ID = 2,
                        appid = 2,
                        AppListing = new AppListing(){ appid = 2, name = "A Whole Lot of aaaaaaaa" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-20),
                        UserID = USERID,
                        ID = 3,
                        appid = 3,
                        AppListing = new AppListing(){ appid = 3, name = OLDESTAPPNAME }
                    },
                });

            var WishlistService = new WishlistService(wlDAMock.Object);

            // Act
            var result = WishlistService.GetWishlistStats(USERID);

            // Assert
            result.Should().NotBeNull();
            result.AvgTimeAdded.Days.Should().BeInRange(12, 13); // 10+13+20 = 37 days / 3 = 12.3 days
            result.AvgTimeBetweenAdded.Days.Should().Be(5); // (3 + 7) / 2 = 5 days
            result.OldestItem.Should().Be(OLDESTAPPNAME);
            result.MostCommonCharacter.Should().Be("a");
        }

        [Fact]
        public void WishlistStatsCanHandleEmptyWishlist()
        {
            // Arrange
            const int USERID = 1;

            var wlDAMock = new Mock<IWishlistItemDA>(MockBehavior.Strict);
            wlDAMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<WishlistItem>());

            var WishlistService = new WishlistService(wlDAMock.Object);

            // Act
            var result = WishlistService.GetWishlistStats(USERID);

            // Assert
            result.Should().NotBeNull();
            result.AvgTimeAdded.Days.Should().Be(0);
            result.AvgTimeBetweenAdded.Days.Should().Be(0);
            result.OldestItem.Should().Be("");
            result.MostCommonCharacter.Should().Be("");
        }

        [Fact]
        public void WishlistStatsEqualCharacterCountPicksOneOfTheMostCommon()
        {
            // Arrange
            const int USERID = 1;

            var wlDAMock = new Mock<IWishlistItemDA>(MockBehavior.Strict);
            wlDAMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
                new List<WishlistItem>()
                {
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-10),
                        UserID = USERID,
                        ID = 1,
                        appid = 1,
                        AppListing = new AppListing(){ appid = 1, name = "aaaa" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-13),
                        UserID = USERID,
                        ID = 2,
                        appid = 2,
                        AppListing = new AppListing(){ appid = 2, name = "bbbb" }
                    },
                    new WishlistItem() {
                        DateAdded = DateTimeOffset.Now.AddDays(-13),
                        UserID = USERID,
                        ID = 3,
                        appid = 3,
                        AppListing = new AppListing(){ appid = 2, name = "ccc" }
                    },
                });

            var WishlistService = new WishlistService(wlDAMock.Object);

            // Act
            var result = WishlistService.GetWishlistStats(USERID);

            // Assert
            result.Should().NotBeNull();
            result.MostCommonCharacter.Should().BeOneOf("a", "b");
            result.MostCommonCharacter.Should().NotBe("c");
        }
    }
}
