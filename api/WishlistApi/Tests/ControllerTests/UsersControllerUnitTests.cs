using Application;
using Application.Commands;
using Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.Helpers;
using WishlistApi.DTOs;

namespace Tests.ControllerTests.UsersController;

public class UsersControllerUnitTests
{
    // GetUserAsync tests
    [Fact]
    public async Task GetUserAsync_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
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
        var returnedUser = Assert.IsType<UserDetailsDTO>(okResult.Value);
        Assert.Equal("John", returnedUser.FirstName);
        Assert.Equal("Doe", returnedUser.LastName);
    }

    // GetAllUsersAsync tests
    [Fact]
    public async Task GetAllUsersAsync_WithValidPage_ReturnsOkResult()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
        var users = new List<UserSummaryDto>
        {
            new UserSummaryDto(Guid.NewGuid(), "user1"),
            new UserSummaryDto(Guid.NewGuid(), "user2")
        };

        fixture.UserServiceMock.Setup(x => x.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(users);
        fixture.SetUserIdentity();

        var controller = fixture.CreateController();

        // Act
        var result = await controller.GetAllUsersAsync(1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var itemsProperty = okResult.Value!.GetType().GetProperty("items")!;
        var items = itemsProperty.GetValue(okResult.Value) as IEnumerable<UserSummaryDto>;
        var itemNames = items!.Select(i => i.Username).ToList();
        itemNames.Should().Contain("user1");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithEmptyList_ReturnsOkResultWithEmptyList()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
        var users = new List<UserSummaryDto>();

        fixture.UserServiceMock.Setup(x => x.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(users);
        fixture.SetUserIdentity();

        var controller = fixture.CreateController();

        // Act
        var result = await controller.GetAllUsersAsync(1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var itemsProperty = okResult.Value!.GetType().GetProperty("items")!;
        var items = itemsProperty.GetValue(okResult.Value) as IEnumerable<UserSummaryDto>;
        items.Should().BeEmpty();
    }

    // GetUserMeAsync tests
    [Fact]
    public async Task GetUserMeAsync_WithValidUser_ReturnsOkResult()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
        var userId = Guid.NewGuid();
        var userDetails = new Domain.UserDetails("John", "Doe", "France", "Paris", "123 Main St", 0);
        var user = new Domain.User(1, "testuser", userId,
            Array.Empty<byte>(), Array.Empty<byte>(), "User", userDetails);

        fixture.UserServiceMock.Setup(x => x.GetUserAsync(It.IsAny<GetUserCommand>()))
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
        var fixture = new UserControllerMockFixture();
        var controller = fixture.CreateController();

        // Act
        var result = await controller.GetUserMeAsync();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // PatchUserAsync tests
    [Fact]
    public async Task PatchUserAsync_PatchMe_Success()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
        fixture.SetUserIdentity();
        fixture.UserServiceMock
            .Setup(x => x.UpdateUserDetailsAsync(It.IsAny<UpdateUserDetailsCommand>()))
            .Returns(Task.CompletedTask);

        var controller = fixture.CreateController();
        var dto = new UserDetailsDTO(
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
        fixture.UserServiceMock.Verify(x => x.UpdateUserDetailsAsync(It.IsAny<UpdateUserDetailsCommand>()), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PatchUserAsync_PatchMe_NoUserIdClaim_Returns500()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
        // Don't set user identity - simulate no authentication claims
        var controller = fixture.CreateController();
        var dto = new UserDetailsDTO(
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
        var fixture = new UserControllerMockFixture();
        fixture.SetUserIdentity();
        fixture.UserServiceMock
            .Setup(x => x.UpdateUserDetailsAsync(It.IsAny<UpdateUserDetailsCommand>()))
            .Returns(Task.CompletedTask);

        var controller = fixture.CreateController();
        var dto = new UserDetailsDTO(
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
        fixture.UserServiceMock.Verify(x => x.UpdateUserDetailsAsync(It.IsAny<UpdateUserDetailsCommand>()), Times.Once);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PatchUserAsync_PatchOtherUser_ConcurrencyConflict_Returns409()
    {
        // Arrange
        var fixture = new UserControllerMockFixture();
        fixture.SetUserIdentity();
        fixture.UserServiceMock
            .Setup(x => x.UpdateUserDetailsAsync(It.IsAny<UpdateUserDetailsCommand>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict"));

        var controller = fixture.CreateController();
        var dto = new UserDetailsDTO(
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
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(409, statusCodeResult.StatusCode);
    }
}
