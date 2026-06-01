using Application;
using Application.Queries;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly WishlistDbContext? _context;
        private readonly IUserService _userService;

        public UserControllerFixture(IUserService userService) => _userService = userService;

        public UserControllerFixture()
        {
            var options = new DbContextOptionsBuilder<WishlistDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WishlistDbContext(options);
            var userRepository = new UserRepository(_context);
            var userQueries = new UserQueries(_context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            _userService = new UserService(userRepository, cache, _context, userQueries);
        }

        public WishlistDbContext GetContext() => _context!;

        public UsersController CreateController() => BuildController(_userService);
    }
}
