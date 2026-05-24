using Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.Helpers;
using UserDetails = WishlistApi.DTOs.UserDTOs.UserDetails;

namespace Tests.ControllerTests.UsersController
{
    public class PatchUserAsyncTests
    {
        [Fact]
        public async Task PatchUserAsync_PatchMe_Success()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            fixture.SetUserIdentity();
            fixture.UserServiceMock
                .Setup(x => x.UpdateUserDetailsAsync(It.IsAny<Application.Commands.UpdateUserDetailsCommand>()))
                .Returns(Task.CompletedTask);

            var controller = fixture.CreateController();
            var dto = new UserDetails(
                RowVersion: 1,
                Email: "test@example.com",
                FirstName: "Updated",
                LastName: "Name",
                Country: "FR",
                City: "Paris",
                Address: "123 Test St"
            );

            // Act
            var result = await controller.PatchUserAsync(dto);

            // Assert
            fixture.UserServiceMock.Verify(x => x.UpdateUserDetailsAsync(It.IsAny<Application.Commands.UpdateUserDetailsCommand>()), Times.Once);
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task PatchUserAsync_PatchMe_NoUserIdClaim_Returns500()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            // Don't set user identity - simulate no authentication claims
            var controller = fixture.CreateController();
            var dto = new UserDetails(
                RowVersion: 1,
                Email: "test@example.com",
                FirstName: "Test",
                LastName: "User",
                Country: null,
                City: null,
                Address: null
            );

            // Act
            var result = await controller.PatchUserAsync(dto);

            // Assert
            Assert.Equal(500, ((ObjectResult)result).StatusCode);
        }

        [Fact]
        public async Task PatchUserAsync_PatchOtherUser_Success()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            fixture.SetUserIdentity();
            fixture.UserServiceMock
                .Setup(x => x.UpdateUserDetailsAsync(It.IsAny<Application.Commands.UpdateUserDetailsCommand>()))
                .Returns(Task.CompletedTask);

            var controller = fixture.CreateController();
            var dto = new UserDetails(
                RowVersion: 5,
                Email: "admin@example.com",
                FirstName: "AdminUpdated",
                LastName: "User",
                Country: "DE",
                City: "Berlin",
                Address: "456 Admin St"
            );

            // Act
            var result = await controller.PatchUserAsync(dto, Guid.NewGuid().ToString());

            // Assert
            fixture.UserServiceMock.Verify(x => x.UpdateUserDetailsAsync(It.IsAny<Application.Commands.UpdateUserDetailsCommand>()), Times.Once);
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task PatchUserAsync_PatchOtherUser_ConcurrencyConflict_Returns409()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            fixture.SetUserIdentity();
            fixture.UserServiceMock
                .Setup(x => x.UpdateUserDetailsAsync(It.IsAny<Application.Commands.UpdateUserDetailsCommand>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict"));

            var controller = fixture.CreateController();
            var dto = new UserDetails(
                RowVersion: 1,
                Email: "test@example.com",
                FirstName: "Test",
                LastName: "User",
                Country: null,
                City: null,
                Address: null
            );

            // Act
            var result = await controller.PatchUserAsync(dto, Guid.NewGuid().ToString());

            // Assert
            Assert.NotNull(result);
            //TODO check 409 status
        }
    }
}