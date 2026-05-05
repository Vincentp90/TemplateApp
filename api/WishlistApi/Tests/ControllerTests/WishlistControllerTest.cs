using DataAccess.AppListings;
using DataAccess.Users;
using DataAccess.Wishlist;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WishlistApi.Controllers;
using WishlistApi.DTOs;

namespace Tests.ControllerTests
{
    public class WishlistControllerTest
    {
        [Fact]
        public async Task GetWishlistTest()
        {
            // Arrange
            Guid externalID = Guid.NewGuid();
            const string APPNAME = "MockAppName";

            var wlDAMock = new Mock<IWishlistItemDA>(MockBehavior.Strict);
            wlDAMock.Setup(x => x.GetWishlistItemsAsync(3)).ReturnsAsync(
                new List<WishlistItem>()
                {
                    new WishlistItem() { 
                        appid = 1, 
                        DateAdded = DateTimeOffset.Now, 
                        ID = 2, 
                        UserID = 3,
                        AppListing = new AppListing(){ appid = 1, name = APPNAME }
                    },
                });

            var userDAMock = new Mock<IUserDA>(MockBehavior.Strict);
            userDAMock.Setup(x => x.GetInternalUserIdAsync(externalID)).ReturnsAsync(3);

            var controller = new WishlistController(wlDAMock.Object, userDAMock.Object, null);

            // Mock authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, externalID.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };


            // Act
            ActionResult<WishlistDTOs.Wishlist> actionResult = await controller.GetWishlistAsync();

            // Assert
            
            // Check data access calls were only called once
            wlDAMock.Verify(x => x.GetWishlistItemsAsync(3), Times.Once);
            userDAMock.Verify(x => x.GetInternalUserIdAsync(externalID), Times.Once);

            actionResult.Should().NotBeNull();
            var okResult = actionResult.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            var wl = okResult!.Value as WishlistDTOs.Wishlist;
            wl.Should().NotBeNull();
            wl.Items.Count().Should().Be(1);

            var item = wl.Items.First() as IDictionary<string, object>;
            item.Should().Contain("appid", 1);
            item["appid"].Should().Be(1);

            item.Should().ContainKey("dateadded");

            item.Should().Contain("name", APPNAME);

            item.Should().NotContainKey("UserID");// Don't return the internal user id


            // Act
            actionResult = await controller.GetWishlistAsync("appid,name"); // Simulate fields=appid,name query param

            // Assert
            actionResult.Should().NotBeNull();
            okResult = actionResult.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            wl = okResult!.Value as WishlistDTOs.Wishlist;
            wl.Should().NotBeNull();
            item = wl.Items.First() as IDictionary<string, object>;

            // Verify that only the specified fields are returned
            item.Should().ContainKey("appid");
            item.Should().ContainKey("name");
            item.Should().NotContainKey("dateadded");
        }
    }
}
