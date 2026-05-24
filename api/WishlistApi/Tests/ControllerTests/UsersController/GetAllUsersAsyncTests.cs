using Application;
using Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tests.Helpers;

namespace Tests.ControllerTests.UsersController;

public class GetAllUsersAsyncTests
{
    [Fact]
    public async Task GetAllUsersAsync_WithValidPage_ReturnsOkResult()
    {
        // Arrange
        var fixture = new UserControllerFixture();
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
        //TODO assert one of the users is actually in the result
    }

    [Fact]
    public async Task GetAllUsersAsync_WithEmptyList_ReturnsOkResultWithEmptyList()
    {
        // Arrange
        var fixture = new UserControllerFixture();
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
        //TODO assert we get empty list back
    }
}