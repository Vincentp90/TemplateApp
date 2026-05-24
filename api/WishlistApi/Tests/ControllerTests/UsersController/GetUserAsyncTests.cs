using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tests.Helpers;
using WishlistApi.DTOs;
using Application.Contracts;

namespace Tests.ControllerTests.UsersController
{
    public class GetUserAsyncTests
    {
        [Fact]
        public async Task GetUserAsync_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var userId = Guid.NewGuid();
            var userDetails = new Domain.UserDetails("John", "Doe", "France", "Paris", "123 Main St", 0);
            var user = new Domain.User(1, "testuser", userId,
                Array.Empty<byte>(), Array.Empty<byte>(), "Admin", userDetails);

            fixture.UserServiceMock.Setup(x => x.GetUserAsync(It.IsAny<GetUserCommand>()))
                .ReturnsAsync(user);
            fixture.SetUserIdentity();

            var controller = fixture.CreateController();

            // Act
            var result = await controller.GetUserAsync(userId.ToString());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDTOs.UserDetails>(okResult.Value);
            Assert.Equal("John", returnedUser.FirstName);
            Assert.Equal("Doe", returnedUser.LastName);
        }
    }
}