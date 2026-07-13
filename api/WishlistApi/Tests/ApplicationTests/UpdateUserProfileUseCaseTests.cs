using Application;
using Application.UseCases.User;
using Application.UseCases.User.Requests;
using Domain;
using Domain.Helpers;
using Domain.Repositories;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Tests.ApplicationTests;

public class UpdateUserProfileUseCaseTests
{
    [Fact]
    public async Task UpdatesUserDetails()
    {
        // Arrange
        var externalUserId = Guid.NewGuid();
        var internalUserId = 42;

        var user = new Domain.User(
            id: internalUserId,
            username: "testuser",
            uuid: externalUserId,
            passwordHash: Array.Empty<byte>(),
            passwordSalt: Array.Empty<byte>(),
            role: "User",
            details: new Domain.UserDetails());

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetInternalUserIdAsync(externalUserId)).ReturnsAsync(internalUserId);
        userRepoMock.Setup(x => x.GetUserAsync(internalUserId)).Returns(Task.FromResult(user));
        userRepoMock.Setup(x => x.UpdateUserAsync(user)).Returns(Task.CompletedTask);

        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var cache = new MemoryCache(new MemoryCacheOptions());

        var useCase = new UpdateUserProfileUseCase(userRepoMock.Object, cache, uowMock.Object);

        // Act
        await useCase.ExecuteAsync(new UpdateUserProfileRequest(
            ExternalUserId: externalUserId,
            RowVersion: 1,
            Name: new FullName("John", "Doe"),
            Location: new Address("US", "NYC", "123 Main St")));

        // Assert
        user.Details.Name.FirstName.Should().Be("John");
        user.Details.Name.LastName.Should().Be("Doe");
        userRepoMock.Verify(x => x.UpdateUserAsync(user), Times.Once);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ThrowsWhenUserNotFound()
    {
        // Arrange
        var externalUserId = Guid.NewGuid();
        var internalUserId = 999;

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetInternalUserIdAsync(externalUserId)).ReturnsAsync(internalUserId);
        userRepoMock.Setup(x => x.GetUserAsync(internalUserId)).Returns(Task.FromResult<User>(null!));

        var cache = new MemoryCache(new MemoryCacheOptions());

        var useCase = new UpdateUserProfileUseCase(userRepoMock.Object, cache, Mock.Of<IUnitOfWork>());

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync(new UpdateUserProfileRequest(
            ExternalUserId: externalUserId,
            RowVersion: 1,
            Name: new FullName("John", "Doe"),
            Location: new Address("US", "NYC", "123 Main St")));
        await act.Should().ThrowAsync<Domain.Exceptions.NotFoundException>()
            .WithMessage("User not found");
    }
}
