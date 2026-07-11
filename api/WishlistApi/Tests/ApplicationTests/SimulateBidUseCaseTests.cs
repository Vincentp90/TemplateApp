using Application;
using Application.Commands;
using Application.UseCases.Auction;
using Application.UseCases.Auction.Requests;
using Application.UseCases.Auth;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Tests.ApplicationTests;

public class SimulateBidUseCaseTests
{
    [Fact]
    public async Task ThrowsWhenNoAuctionExists()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
        auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync((Auction?)null);

        var configMock = new Mock<IConfiguration>(MockBehavior.Strict);
        configMock.Setup(c => c["SimUserPassword"]).Returns("simpassword");

        var userRepoMock = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepoMock.Setup(x => x.GetUserAsync("SimulateAuctionUser")).ReturnsAsync((Domain.User?)null);
        userRepoMock.Setup(x => x.IsUsernameAvailableAsync("SimulateAuctionUser")).ReturnsAsync(true);
        userRepoMock.Setup(x => x.AddUser(It.IsAny<Domain.User>())).Verifiable();

        var useCase = new SimulateBidUseCase(
            auctionRepoMock.Object,
            Mock.Of<IUnitOfWork>(),
            new LoginUserUseCase(userRepoMock.Object),
            new PlaceBidUseCase(Mock.Of<IAuctionRepository>(), Mock.Of<IUnitOfWork>()),
            userRepoMock.Object,
            configMock.Object);

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync();
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("No auction found for simulation.");
    }

    [Fact]
    public async Task ThrowsWhenSimUserPasswordNotConfigured()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);

        var configMock = new Mock<IConfiguration>(MockBehavior.Strict);
        configMock.Setup(c => c["SimUserPassword"]).Returns((string?)null);

        var useCase = new SimulateBidUseCase(
            auctionRepoMock.Object,
            Mock.Of<IUnitOfWork>(),
            new LoginUserUseCase(Mock.Of<IUserRepository>()),
            new PlaceBidUseCase(Mock.Of<IAuctionRepository>(), Mock.Of<IUnitOfWork>()),
            Mock.Of<IUserRepository>(),
            configMock.Object);

        // Act & assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync());
        exception.Message.Should().Be("SimUserPassword configuration is missing.");
    }
}
