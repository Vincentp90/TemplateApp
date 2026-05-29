using Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using WishlistApi.Controllers;

namespace Tests.Helpers
{
    public abstract class UserControllerFixtureBase
    {
        protected readonly DefaultHttpContext HttpContext = new();

        public void SetUserIdentity(string userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, "User")
            };
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        public void SetUserIdentity() => SetUserIdentity(Guid.NewGuid().ToString());

        protected UsersController BuildController(IUserService service)
        {
            return new UsersController(service)
            {
                ControllerContext = new ControllerContext { HttpContext = HttpContext }
            };
        }
    }

    // Unit tests
    public class UserControllerMockFixture : UserControllerFixtureBase
    {
        public Mock<IUserService> UserServiceMock { get; } = new(MockBehavior.Strict);
        public UsersController CreateController() => BuildController(UserServiceMock.Object);
    }

    // Integration tests
    public class UserControllerFixture : UserControllerFixtureBase
    {
        private readonly IUserService _userService;
        public UserControllerFixture(IUserService userService) => _userService = userService;
        public UsersController CreateController() => BuildController(_userService);
    }
}
