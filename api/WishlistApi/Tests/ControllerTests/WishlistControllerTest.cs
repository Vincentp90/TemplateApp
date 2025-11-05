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

            var controller = new WishlistController(wlDAMock.Object, userDAMock.Object);

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
            var actionResult = await controller.GetWishlistAsync() as OkObjectResult;

            // Assert
            actionResult.Should().NotBeNull();
            wlDAMock.Verify(x => x.GetWishlistItemsAsync(3), Times.Once);
            userDAMock.Verify(x => x.GetInternalUserIdAsync(externalID), Times.Once);

            var wl = actionResult.Value as IEnumerable<ExpandoObject>;
            wl.Should().NotBeNull();
            wl.Count().Should().Be(1);

            var item = wl.First() as IDictionary<string, object>;
            item.Should().Contain("appid", 1);
            item["appid"].Should().Be(1);

            item.Should().ContainKey("dateadded");

            item.Should().Contain("name", APPNAME);

            item.Should().NotContainKey("UserID");// Don't return the internal user id


            // Act
            actionResult = await controller.GetWishlistAsync("appid,name") as OkObjectResult; // Simulate fields=appid,name query param

            // Assert
            actionResult.Should().NotBeNull();
            wl = actionResult.Value as IEnumerable<ExpandoObject>;
            wl.Should().NotBeNull();
            item = wl.First() as IDictionary<string, object>;

            // Verify that only the specified fields are returned
            item.Should().ContainKey("appid");
            item.Should().ContainKey("name");
            item.Should().NotContainKey("dateadded");
        }
    }
}
