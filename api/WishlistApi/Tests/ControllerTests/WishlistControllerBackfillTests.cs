using Application.Events;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WishlistApi.Controllers;
using WishlistApi.Helpers;

namespace Tests.ControllerTests;

/// <summary>
/// TDD tests for the POST /_backfill endpoint.
/// These tests verify that the backfill endpoint publishes WishlistItemAdded events
/// for all items in a user's wishlist (or a single item when appId is specified).
/// </summary>
public class WishlistControllerBackfillTests
{
    private const int USERID = 42;
    private static readonly Guid EXTERNAL_GUID = new Guid("00000000-0000-0000-0000-000000000042");

    private static (Mock<IWishlistItemRepository>, Mock<IPublishBackfillEventUseCase>, WishlistController, DefaultHttpContext) CreateSut(
        List<Domain.WishlistItem> items)
    {
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, EXTERNAL_GUID.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetInternalUserIdAsync(EXTERNAL_GUID)).ReturnsAsync(USERID);

        var userContextMock = new UserContext(mockAccessor.Object, userRepoMock.Object);

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(items);

        var getWishlistUseCaseMock = new Mock<IGetWishlistUseCase>();
        getWishlistUseCaseMock.Setup(x => x.ExecuteAsync(It.IsAny<GetWishlistRequest>())).ReturnsAsync(items);

        var publishBackfillEventUseCaseMock = new Mock<IPublishBackfillEventUseCase>();
        publishBackfillEventUseCaseMock.Setup(p => p.ExecuteAsync(It.IsAny<PublishBackfillEventRequest>())).Returns(Task.CompletedTask);

        var addWishlistItemUseCaseMock = new Mock<IAddWishlistItemUseCase>();
        var deleteWishlistItemUseCaseMock = new Mock<IDeleteWishlistItemUseCase>();
        var getWishlistStatsUseCaseMock = new Mock<IGetWishlistStatsUseCase>();
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
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return (repositoryMock, publishBackfillEventUseCaseMock, controller, httpContext);
    }

    #region Backfill endpoint tests

    [Fact]
    public async Task BackfillAsync_publishesOneEventPerWishlistItem()
    {
        // Arrange
        var items = new List<Domain.WishlistItem>
        {
            new(1, 100, "Game A", DateTimeOffset.UtcNow, USERID),
            new(2, 200, "Game B", DateTimeOffset.UtcNow, USERID),
            new(3, 300, "Game C", DateTimeOffset.UtcNow, USERID)
        };

        var (_, publishBackfillEventUseCaseMock, controller, _) = CreateSut(items);

        // Act
        var result = await controller.BackfillAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(202);
        objectResult.Value.Should().NotBeNull();

        publishBackfillEventUseCaseMock.Verify(
            p => p.ExecuteAsync(It.IsAny<PublishBackfillEventRequest>()),
            Times.Exactly(3));

        var publishedRequests = publishBackfillEventUseCaseMock.Invocations
            .Where(i => i.Method.Name == "ExecuteAsync")
            .Select(i => i.Arguments[0])
            .Cast<PublishBackfillEventRequest>()
            .ToList();

        publishedRequests.Should().HaveCount(3);
        publishedRequests.Should().ContainSingle(ev => ev.AppId == 100);
        publishedRequests.Should().ContainSingle(ev => ev.AppId == 200);
        publishedRequests.Should().ContainSingle(ev => ev.AppId == 300);
    }

    [Fact]
    public async Task BackfillAsync_emptyWishlist_returnsAcceptedWithZeroCount()
    {
        // Arrange
        var items = new List<Domain.WishlistItem>();

        var (_, publishBackfillEventUseCaseMock, controller, _) = CreateSut(items);

        // Act
        var result = await controller.BackfillAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(202);

        publishBackfillEventUseCaseMock.Verify(
            p => p.ExecuteAsync(It.IsAny<PublishBackfillEventRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task BackfillAsync_publishesCorrectUserIdAndAppId()
    {
        // Arrange
        var items = new List<Domain.WishlistItem>
        {
            new(1, 42, "TestGame", new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero), USERID)
        };

        var (_, publishBackfillEventUseCaseMock, controller, _) = CreateSut(items);

        // Act
        await controller.BackfillAsync();

        // Assert
        publishBackfillEventUseCaseMock.Verify(
            p => p.ExecuteAsync(It.IsAny<PublishBackfillEventRequest>()),
            Times.Once);

        var publishedRequest = publishBackfillEventUseCaseMock.Invocations
            .Where(i => i.Method.Name == "ExecuteAsync")
            .Select(i => i.Arguments[0])
            .Cast<PublishBackfillEventRequest>()
            .Single();

        publishedRequest.UserId.Should().Be(USERID);
        publishedRequest.AppId.Should().Be(42);
        publishedRequest.DateAdded.Should().Be(new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero));
    }

    #endregion
}
