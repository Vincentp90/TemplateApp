using Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using WishlistApi.Controllers;

namespace Tests.Helpers
{
    /// <summary>
    /// Test fixture for UsersController that provides mock HttpContext and IUserService.
    /// </summary>
    public class UserControllerFixture
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly DefaultHttpContext _httpContext;

        public Mock<IUserService> UserServiceMock => _userServiceMock;
        public DefaultHttpContext HttpContext => _httpContext;

        public UserControllerFixture()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
            _userServiceMock = new Mock<IUserService>(MockBehavior.Strict);
            _httpContext = new DefaultHttpContext();

            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
        }

        /// <summary>
        /// Sets the authenticated user identity using the provided GUID.
        /// </summary>
        public void SetUserIdentity(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);
        }

        public void SetUserIdentity()
        {
            SetUserIdentity(Guid.NewGuid().ToString());
        }

        public UsersController CreateController()
        {
            var controller = new UsersController(_userServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            return controller;
        }
    }
}