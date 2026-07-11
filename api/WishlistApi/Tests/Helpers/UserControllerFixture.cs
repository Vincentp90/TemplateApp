using Application.Contracts;
using Application.UseCases.User;
using Application.UseCases.User.Requests;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Users;
using Infrastructure.ReadAdapters;
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

        protected UsersController BuildController(
            IGetUserProfileUseCase getUserProfileUseCase,
            IUpdateUserProfileUseCase updateUserProfileUseCase,
            IGetPaginatedUsersUseCase getPaginatedUsersUseCase)
        {
            return new UsersController(getUserProfileUseCase, updateUserProfileUseCase, getPaginatedUsersUseCase)
            {
                ControllerContext = new ControllerContext { HttpContext = HttpContext }
            };
        }
    }

    // Unit tests
    public class UserControllerMockFixture : UserControllerFixtureBase
    {
        public Mock<IGetUserProfileUseCase> GetUserProfileUseCaseMock { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateUserProfileUseCase> UpdateUserProfileUseCaseMock { get; } = new(MockBehavior.Strict);
        public Mock<IGetPaginatedUsersUseCase> GetPaginatedUsersUseCaseMock { get; } = new(MockBehavior.Strict);

        public UsersController CreateController() => BuildController(
            GetUserProfileUseCaseMock.Object,
            UpdateUserProfileUseCaseMock.Object,
            GetPaginatedUsersUseCaseMock.Object);
    }

    // Integration tests
    public class UserControllerFixture : UserControllerFixtureBase
    {
        private readonly WishlistDbContext? _context;
        private readonly IGetUserProfileUseCase _getUserProfileUseCase;
        private readonly IUpdateUserProfileUseCase _updateUserProfileUseCase;
        private readonly IGetPaginatedUsersUseCase _getPaginatedUsersUseCase;

        public UserControllerFixture(IGetUserProfileUseCase getUserProfileUseCase, IUpdateUserProfileUseCase updateUserProfileUseCase, IGetPaginatedUsersUseCase getPaginatedUsersUseCase)
        {
            _getUserProfileUseCase = getUserProfileUseCase;
            _updateUserProfileUseCase = updateUserProfileUseCase;
            _getPaginatedUsersUseCase = getPaginatedUsersUseCase;
        }

        public UserControllerFixture()
        {
            var options = new DbContextOptionsBuilder<WishlistDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new WishlistDbContext(options);
            var userRepository = new UserRepository(_context);
            var userReadModel = new UserReadAdapter(_context);
            var cache = new MemoryCache(new MemoryCacheOptions());
            _getUserProfileUseCase = new GetUserProfileUseCase(userRepository, cache);
            _updateUserProfileUseCase = new UpdateUserProfileUseCase(userRepository, cache, _context);
            _getPaginatedUsersUseCase = new GetPaginatedUsersUseCase(userReadModel);
        }

        public WishlistDbContext GetContext() => _context!;

        public UsersController CreateController() => BuildController(_getUserProfileUseCase, _updateUserProfileUseCase, _getPaginatedUsersUseCase);
    }
}
