using Application;
using Application.Contracts;
using FluentAssertions;
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
        var itemsProperty = okResult.Value!.GetType().GetProperty("items")!;
        var items = itemsProperty.GetValue(okResult.Value) as IEnumerable<UserSummaryDto>;
        var itemNames = items!.Select(i => i.Username).ToList();
        itemNames.Should().Contain("user1");
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
        var itemsProperty = okResult.Value!.GetType().GetProperty("items")!;
        var items = itemsProperty.GetValue(okResult.Value) as IEnumerable<UserSummaryDto>;
        items.Should().BeEmpty();
    }
}