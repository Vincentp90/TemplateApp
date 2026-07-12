using Application;
using Application.UseCases.Auth;
using Application.UseCases.Auth.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class RegisterUserUseCaseTests
{
    [Fact]
    public async Task CreatesUserWithHashedPassword()
    {
        // Arrange
        const string username = "newuser";
        const string password = "securepass123";

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.IsUsernameAvailableAsync(username)).ReturnsAsync(true);

        var capturedUser = default(Domain.User);
        userRepoMock
            .Setup(x => x.AddUser(It.IsAny<Domain.User>()))
            .Callback<Domain.User>(u => capturedUser = u);

        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var useCase = new RegisterUserUseCase(userRepoMock.Object, uowMock.Object);

        // Act
        await useCase.ExecuteAsync(new RegisterUserRequest(username, password));

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Username.Should().Be(username);
        capturedUser.PasswordHash.Should().NotBeNull();
        capturedUser.PasswordHash.Length.Should().Be(32);
        capturedUser.PasswordSalt.Should().NotBeNull();
        capturedUser.PasswordSalt.Length.Should().Be(16);
        userRepoMock.Verify(x => x.AddUser(capturedUser), Times.Once);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ThrowsWhenUsernameAlreadyTaken()
    {
        // Arrange
        const string username = "takenuser";

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.IsUsernameAvailableAsync(username)).ReturnsAsync(false);

        var useCase = new RegisterUserUseCase(userRepoMock.Object, Mock.Of<IUnitOfWork>());

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync(new RegisterUserRequest(username, "password"));
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Username already taken");

        userRepoMock.Verify(x => x.AddUser(It.IsAny<Domain.User>()), Times.Never);
    }
}
