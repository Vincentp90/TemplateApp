using Application;
using System.Security.Cryptography;
using Application.UseCases.Auth;
using Application.UseCases.Auth.Requests;
using Domain;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class LoginUserUseCaseTests
{
    [Fact]
    public async Task ReturnsLoginResult_WhenCredentialsValid()
    {
        // Arrange
        const string username = "testuser";
        const string password = "correctpass";

        byte[] passwordHash;
        byte[] passwordSalt;
        PasswordHelper.CreatePasswordHash(password, out passwordHash, out passwordSalt);

        var user = new Domain.User(
            id: 1,
            username: username,
            uuid: Guid.NewGuid(),
            passwordHash: passwordHash,
            passwordSalt: passwordSalt,
            role: "User",
            details: new Domain.UserDetails());

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetUserAsync(username)).ReturnsAsync(user);

        var useCase = new LoginUserUseCase(userRepoMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new LoginUserRequest(username, password));

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be(username);
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task ReturnsNull_WhenUserNotFound()
    {
        // Arrange
        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetUserAsync("nonexistent")).ReturnsAsync((Domain.User?)null);

        var useCase = new LoginUserUseCase(userRepoMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new LoginUserRequest("nonexistent", "password"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReturnsNull_WhenPasswordWrong()
    {
        // Arrange
        byte[] passwordHash;
        byte[] passwordSalt;
        PasswordHelper.CreatePasswordHash("correctpass", out passwordHash, out passwordSalt);

        var user = new Domain.User(
            id: 1,
            username: "testuser",
            uuid: Guid.NewGuid(),
            passwordHash: passwordHash,
            passwordSalt: passwordSalt,
            role: "User",
            details: new Domain.UserDetails());

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetUserAsync("testuser")).ReturnsAsync(user);

        var useCase = new LoginUserUseCase(userRepoMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new LoginUserRequest("testuser", "wrongpass"));

        // Assert
        result.Should().BeNull();
    }
}
