using Application;
using Application.UseCases.User;
using Application.UseCases.User.Requests;
using Domain;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Tests.ApplicationTests;

public class GetUserProfileUseCaseTests
{
    [Fact]
    public async Task ReturnsUser_WhenFound()
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
        userRepoMock.Setup(x => x.GetUserAsync(internalUserId)).ReturnsAsync(user);

        var cache = new MemoryCache(new MemoryCacheOptions());

        var useCase = new GetUserProfileUseCase(userRepoMock.Object, cache);

        // Act
        var result = await useCase.ExecuteAsync(new GetUserProfileRequest(externalUserId));

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.UUID.Should().Be(externalUserId);
    }

    [Fact]
    public async Task UsesCachedInternalId()
    {
        // Arrange
        var externalUserId = Guid.NewGuid();
        var internalUserId = 42;

        var user = new Domain.User(
            id: internalUserId,
            username: "cacheduser",
            uuid: externalUserId,
            passwordHash: Array.Empty<byte>(),
            passwordSalt: Array.Empty<byte>(),
            role: "User",
            details: new Domain.UserDetails());

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        // Set cache first
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set(externalUserId, internalUserId, new MemoryCacheEntryOptions { Size = 1 });
        // Repo should not be called for the ID lookup
        userRepoMock.Setup(x => x.GetUserAsync(internalUserId)).ReturnsAsync(user);

        var useCase = new GetUserProfileUseCase(userRepoMock.Object, cache);

        // Act
        var result = await useCase.ExecuteAsync(new GetUserProfileRequest(externalUserId));

        // Assert
        result.Should().NotBeNull();
        userRepoMock.Verify(x => x.GetInternalUserIdAsync(externalUserId), Times.Never);
    }

    [Fact]
    public async Task ThrowsWhenUserNotFound()
    {
        // Arrange
        var externalUserId = Guid.NewGuid();
        var internalUserId = 999;

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetInternalUserIdAsync(externalUserId)).ReturnsAsync(internalUserId);
        userRepoMock.Setup(x => x.GetUserAsync(internalUserId)).ReturnsAsync((Domain.User?)null);

        var cache = new MemoryCache(new MemoryCacheOptions());

        var useCase = new GetUserProfileUseCase(userRepoMock.Object, cache);

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync(new GetUserProfileRequest(externalUserId));
        await act.Should().ThrowAsync<Domain.Exceptions.NotFoundException>()
            .WithMessage("User not found");
    }
}
