using Application.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using WishlistApi.DTOs;

namespace Tests.ControllerTests;

public class UsersControllerIntegrationTests
{
    [Fact]
    public async Task GetUserMeAsync_ReturnsUserDetails()
    {
        // Arrange
        var fixture = new UserControllerFixture();
        var userId = Guid.NewGuid();
        var username = "integrationuser";
        SeedUser(fixture.GetContext(), userId, username);

        fixture.SetUserIdentity(userId.ToString());
        var controller = fixture.CreateController();

        // Act
        var result = await controller.GetUserMeAsync();

        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var dto = okResult.Value as UserDetailsDTO;
        dto.Should().NotBeNull();
        dto!.Email.Should().Be(username);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUserDetails()
    {
        // Arrange
        var fixture = new UserControllerFixture();
        var userId = Guid.NewGuid();
        var username = "targetuser";
        SeedUser(fixture.GetContext(), userId, username);

        fixture.SetUserIdentity(Guid.NewGuid().ToString());
        var controller = fixture.CreateController();

        // Act
        var result = await controller.GetUserAsync(userId.ToString());

        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var dto = okResult.Value as UserDetailsDTO;
        dto.Should().NotBeNull();
        dto!.Email.Should().Be(username);
    }

    [Fact]
    public async Task PatchUserAsync_PatchMe_UpdatesUserDetails()
    {
        // Arrange
        var fixture = new UserControllerFixture();
        var userId = Guid.NewGuid();
        var username = "patchuser";
        SeedUser(fixture.GetContext(), userId, username);

        fixture.SetUserIdentity(userId.ToString());
        var controller = fixture.CreateController();

        var dto = new UserDetailsDTO(
            RowVersion: 0,
            Email: "updated@example.com",
            FirstName: "Updated",
            LastName: "Name",
            Country: "FR",
            City: "Paris",
            Address: "456 Patch St"
        );

        // Act
        var result = await controller.PatchUserAsync(dto);

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkResult>();
    }

    [Fact]
    public async Task PatchUserAsync_PatchOtherUser_UpdatesUserDetails()
    {
        // Arrange
        var fixture = new UserControllerFixture();
        var targetUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var username = "othertarget";
        SeedUser(fixture.GetContext(), targetUserId, username);

        fixture.SetUserIdentity(adminUserId.ToString());
        var controller = fixture.CreateController();

        var dto = new UserDetailsDTO(
            RowVersion: 0,
            Email: "adminupdated@example.com",
            FirstName: "Admin",
            LastName: "Update",
            Country: "DE",
            City: "Berlin",
            Address: "789 Admin Ave"
        );

        // Act
        var result = await controller.PatchUserAsync(dto, targetUserId.ToString());

        // Assert
        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkResult>();
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsPaginatedUsers()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseInMemoryDatabase("GetAllUsersAsync")
            .Options;

        var fixture = new UserControllerFixture();
        SeedMultipleUsers(fixture.GetContext(), 5);

        fixture.SetUserIdentity(Guid.NewGuid().ToString());
        var controller = fixture.CreateController();

        // Act
        var result = await controller.GetAllUsersAsync(1, 10);

        // Assert
        var okResult = result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var dynamicObj = okResult.Value;
        var itemsProp = dynamicObj!.GetType().GetProperty("items");
        var items = itemsProp!.GetValue(dynamicObj) as IEnumerable<UserSummaryDto>;
        items.Should().NotBeNullOrEmpty();
        items!.Should().HaveCount(5);
    }

    private static void SeedUser(WishlistDbContext context, Guid userId, string username)
    {
        var user = new User
        {
            ID = 1,
            UUID = userId,
            Username = username,
            PasswordHash = Array.Empty<byte>(),
            PasswordSalt = Array.Empty<byte>(),
            Role = "User"
        };

        context.Users.Add(user);

        var userDetails = new UserDetails
        {
            ID = 1,
            UserID = 1,
            User = user,
            FirstName = "First",
            LastName = "Last"
        };

        context.UserDetails.Add(userDetails);
        context.SaveChanges();
    }

    private static void SeedMultipleUsers(WishlistDbContext context, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var user = new User
            {
                ID = i,
                UUID = Guid.NewGuid(),
                Username = $"user{i}",
                PasswordHash = Array.Empty<byte>(),
                PasswordSalt = Array.Empty<byte>(),
                Role = "User"
            };
            context.Users.Add(user);

            var userDetails = new UserDetails
            {
                ID = i,
                UserID = i,
                User = user,
                FirstName = $"First{i}",
                LastName = $"Last{i}"
            };
            context.UserDetails.Add(userDetails);
        }
        context.SaveChanges();
    }
}
