using Application;
using Application.Commands;
using Application.Events;
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

    private static (Mock<IWishlistItemRepository>, Mock<IEventPublisher>, WishlistController, DefaultHttpContext) CreateSut(
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

        var userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
        userServiceMock.Setup(x => x.GetInternalUserIdAsync(EXTERNAL_GUID)).Returns(new ValueTask<int>(USERID));

        var userContextMock = new UserContext(mockAccessor.Object, userServiceMock.Object);

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(items);

        var eventPublisherMock = new Mock<IEventPublisher>(MockBehavior.Strict);
        eventPublisherMock.Setup(p => p.PublishAsync(It.IsAny<object>())).Returns(Task.CompletedTask);

        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var service = new WishlistService(repositoryMock.Object, uowMock.Object, eventPublisherMock.Object);
        var controller = new WishlistController(userContextMock, service);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return (repositoryMock, eventPublisherMock, controller, httpContext);
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

        var (_, eventPublisherMock, controller, _) = CreateSut(items);

        // Act
        var result = await controller.BackfillAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(202);
        objectResult.Value.Should().NotBeNull();

        eventPublisherMock.Verify(
            p => p.PublishAsync(It.IsAny<object>()),
            Times.Exactly(3));

        var publishedEvents = eventPublisherMock.Invocations
            .Where(i => i.Method.Name == "PublishAsync")
            .Select(i => i.Arguments[0])
            .Cast<WishlistItemAdded>()
            .ToList();

        publishedEvents.Should().HaveCount(3);
        publishedEvents.Should().ContainSingle(ev => ev.AppId == 100);
        publishedEvents.Should().ContainSingle(ev => ev.AppId == 200);
        publishedEvents.Should().ContainSingle(ev => ev.AppId == 300);
    }

    [Fact]
    public async Task BackfillAsync_emptyWishlist_returnsAcceptedWithZeroCount()
    {
        // Arrange
        var items = new List<Domain.WishlistItem>();

        var (_, eventPublisherMock, controller, _) = CreateSut(items);

        // Act
        var result = await controller.BackfillAsync();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(202);

        eventPublisherMock.Verify(
            p => p.PublishAsync(It.IsAny<object>()),
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

        var (_, eventPublisherMock, controller, _) = CreateSut(items);

        // Act
        await controller.BackfillAsync();

        // Assert
        eventPublisherMock.Verify(
            p => p.PublishAsync(It.IsAny<object>()),
            Times.Once);

        var publishedEvent = eventPublisherMock.Invocations
            .Where(i => i.Method.Name == "PublishAsync")
            .Select(i => i.Arguments[0])
            .Cast<WishlistItemAdded>()
            .Single();

        publishedEvent.UserId.Should().Be(USERID.ToString());
        publishedEvent.AppId.Should().Be(42);
        publishedEvent.AddedAt.Should().Be(new DateTimeOffset(2024, 3, 15, 12, 0, 0, TimeSpan.Zero));
    }

    #endregion
}
