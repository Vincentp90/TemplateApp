using Application;
using Application.Contracts;
using Application.Events;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain.Repositories;
using FluentAssertions;
using Infrastructure.SteamTracker;
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
            var alertProxyMock = new Mock<ISteamTrackerAlertProxy>();
            alertProxyMock.Setup(x => x.GetAlertRulesAsync(It.IsAny<string>())).ReturnsAsync(new List<Application.Contracts.AlertRuleInfo>());

            var controller = new WishlistController(
                userContextMock,
                getWishlistUseCaseMock.Object,
                addWishlistItemUseCaseMock.Object,
                deleteWishlistItemUseCaseMock.Object,
                getWishlistStatsUseCaseMock.Object,
                publishBackfillEventUseCaseMock.Object,
                setAlertRuleUseCaseMock.Object,
                deleteAlertRuleUseCaseMock.Object,
                alertProxyMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var actionResult = await controller.GetWishlistAsync();

            // Assert - returns IQueryable<WishlistItemDto> via OData [EnableQuery]
            actionResult.Should().NotBeNull();
            var okResult = actionResult.Result as OkObjectResult ?? throw new Exception("Expected OkObjectResult");
            okResult.Should().NotBeNull();

            var queryable = okResult.Value as IQueryable<WishlistItemDto>;
            queryable.Should().NotBeNull("Expected IQueryable<WishlistItemDto>");

            var items = queryable!.ToList();
            items.Count.Should().Be(1);

            var item = items[0];
            item.AppId.Should().Be(1);
            item.Name.Should().Be(APPNAME);
            item.DateAdded.Should().NotBeNull();
            item.AlertRuleId.Should().BeNull();

            // Verify use case was called once
            getWishlistUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<GetWishlistRequest>()), Times.Once);
        }
    }
}
