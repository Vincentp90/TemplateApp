using Application;
using Application.Contracts;
using Application.Events;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.SharedDb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using WishlistApi.Controllers;
using WishlistApi.Helpers;
namespace Tests.ControllerTests
{
    //TODO how to organise a test like this. It's not a unit test since it tests 2 layers (API + application)
    // but it's not really a full integration test either since it doesn't even go to the DB
    public class WishlistControllerTest
    {
        [Fact]
        public async Task GetWishlistTest()
        {
            // Arrange
            Guid externalID = Guid.NewGuid();
            const string APPNAME = "MockAppName";

            // Mock authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, externalID.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = user };

            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
            userRepoMock.Setup(x => x.GetInternalUserIdAsync(externalID)).ReturnsAsync(3);

            IUserContext userContextMock = new UserContext(mockAccessor.Object, userRepoMock.Object);

            var eventPublisherMock = new Mock<IEventPublisher>();

            var getWishlistUseCaseMock = new Mock<IGetWishlistUseCase>();
            getWishlistUseCaseMock.Setup(x => x.ExecuteAsync(It.IsAny<GetWishlistRequest>())).ReturnsAsync(
                new List<Domain.WishlistItem>()
                {
                    new Domain.WishlistItem(
                        id: 2,
                        appId: 1,
                        name: APPNAME,
                        dateAdded: DateTimeOffset.Now,
                        userId: 3
                    ),
                });

            var addWishlistItemUseCaseMock = new Mock<IAddWishlistItemUseCase>();
            var deleteWishlistItemUseCaseMock = new Mock<IDeleteWishlistItemUseCase>();
            var getWishlistStatsUseCaseMock = new Mock<IGetWishlistStatsUseCase>();
            var publishBackfillEventUseCaseMock = new Mock<IPublishBackfillEventUseCase>();
            var setAlertRuleUseCaseMock = new Mock<ISetAlertRuleUseCase>();
            var deleteAlertRuleUseCaseMock = new Mock<IDeleteAlertRuleUseCase>();

            var controller = new WishlistController(
                userContextMock,
                getWishlistUseCaseMock.Object,
                addWishlistItemUseCaseMock.Object,
                deleteWishlistItemUseCaseMock.Object,
                getWishlistStatsUseCaseMock.Object,
                publishBackfillEventUseCaseMock.Object,
                setAlertRuleUseCaseMock.Object,
                deleteAlertRuleUseCaseMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            ActionResult<Wishlist> actionResult = await controller.GetWishlistAsync();

            // Assert - returns items with core fields only (no price/alert enrichment)
            actionResult.Should().NotBeNull();
            var okResult = actionResult.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            var wl = okResult!.Value as Wishlist;
            wl.Should().NotBeNull();
            wl.Items.Count().Should().Be(1);

            var item = wl.Items.First();
            item.AppId.Should().Be(1);
            item.DateAdded.Should().NotBeNull();
            item.Name.Should().Be(APPNAME);


            // Act
            actionResult = await controller.GetWishlistAsync("appid,name"); // Simulate fields=appid,name query param

            // Assert
            actionResult.Should().NotBeNull();
            okResult = actionResult.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            wl = okResult!.Value as Wishlist;
            wl.Should().NotBeNull();
            item = wl.Items.First();

            // Verify all core fields are returned (controller no longer filters by fields)
            item.AppId.Should().NotBeNull();
            item.Name.Should().NotBeNull();
            item.DateAdded.Should().NotBeNull();
        }
    }
}
