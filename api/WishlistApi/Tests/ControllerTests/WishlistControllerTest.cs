using Application;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using WishlistApi.Controllers;
using WishlistApi.DTOs;
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

            var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
            repositoryMock.Setup(x => x.GetWishlistItemsAsync(3)).ReturnsAsync(
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

            var userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
            userServiceMock.Setup(x => x.GetInternalUserIdAsync(externalID)).ReturnsAsync(3);

            IUserContext userContextMock = new UserContext(mockAccessor.Object, userServiceMock.Object);

            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var controller = new WishlistController(userContextMock, new WishlistService(repositoryMock.Object, uowMock.Object));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            ActionResult<Wishlist> actionResult = await controller.GetWishlistAsync();

            // Assert
            
            // Check data access calls were only called once
            repositoryMock.Verify(x => x.GetWishlistItemsAsync(3), Times.Once);
            userServiceMock.Verify(x => x.GetInternalUserIdAsync(externalID), Times.Once);

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

            // Verify that only the specified fields are returned
            item.AppId.Should().NotBeNull();            
            item.Name.Should().NotBeNull();
            item.DateAdded.Should().BeNull();
        }
    }
}
