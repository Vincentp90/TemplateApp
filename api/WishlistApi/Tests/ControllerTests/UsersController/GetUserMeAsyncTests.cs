using Application;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tests.Helpers;
using WishlistApi.DTOs;

namespace Tests.ControllerTests.UsersController
{
    public class GetUserMeAsyncTests
    {
        [Fact]
        public async Task GetUserMeAsync_WithValidUser_ReturnsOkResult()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var userId = Guid.NewGuid();
            var userDetails = new Domain.UserDetails("John", "Doe", "France", "Paris", "123 Main St", 0);
            var user = new Domain.User(1, "testuser", userId,
                Array.Empty<byte>(), Array.Empty<byte>(), "User", userDetails);

            fixture.UserServiceMock.Setup(x => x.GetUserAsync(It.IsAny<Application.Commands.GetUserCommand>()))
                .ReturnsAsync(user);
            fixture.SetUserIdentity(userId.ToString());

            var controller = fixture.CreateController();

            // Act
            var result = await controller.GetUserMeAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDetailsDTO>(okResult.Value);
            Assert.Equal("John", returnedUser.FirstName);
            Assert.Equal("Doe", returnedUser.LastName);
        }

        [Fact]
        public async Task GetUserMeAsync_NoUserIdClaim_Returns500()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var controller = fixture.CreateController();

            // Act
            var result = await controller.GetUserMeAsync();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }
    }
}