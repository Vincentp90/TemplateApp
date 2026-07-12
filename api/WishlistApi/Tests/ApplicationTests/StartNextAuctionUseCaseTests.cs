using Application.UseCases.AppListing;
using Application.UseCases.Auction;
using Application.UseCases.Auction.Requests;
using Domain;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class StartNextAuctionUseCaseTests
{
    [Fact]
    public async Task StartsNextAuction_WhenExistingAuction()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var existingAuction = Domain.Auction.FromData(
            id: 1,
            dateAdded: DateTimeOffset.Now.AddDays(-1),
            currentPrice: 50,
            startingPrice: 10,
            status: AuctionStatus.Open,
            userId: null,
            appListingId: 1,
            rowVersion: 1
        );
        auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(existingAuction);
        auctionRepoMock.Setup(x => x.CloseAuctionAndAddNewAsync(It.IsAny<Auction>())).Returns(Task.CompletedTask);

        var getRandomUseCase = new GetRandomAppListingUseCase(Mock.Of<IAppListingRepository>());
        // Override: we need a mock for the random listing
        var randomListingRepo = new Mock<IAppListingRepository>();
        randomListingRepo.Setup(x => x.GetRandomAsync()).ReturnsAsync(new AppListing(42, "Random Game"));
        var randomUseCase = new GetRandomAppListingUseCase(randomListingRepo.Object);

        var useCase = new StartNextAuctionUseCase(auctionRepoMock.Object, uowMock.Object, randomUseCase);

        // Act
        await useCase.ExecuteAsync(new StartNextAuctionRequest());

        // Assert
        auctionRepoMock.Verify(x => x.GetLatestAuctionAsync(), Times.Once);
        auctionRepoMock.Verify(x => x.CloseAuctionAndAddNewAsync(It.IsAny<Auction>()), Times.Once);
        auctionRepoMock.Verify(x => x.AddAuction(It.IsAny<Auction>()), Times.Never);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddsNewAuction_WhenNoExistingAuction()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync((Auction?)null);
        auctionRepoMock.Setup(x => x.AddAuction(It.IsAny<Auction>())).Verifiable();

        var randomListingRepo = new Mock<IAppListingRepository>();
        randomListingRepo.Setup(x => x.GetRandomAsync()).ReturnsAsync(new AppListing(99, "First Game"));
        var randomUseCase = new GetRandomAppListingUseCase(randomListingRepo.Object);

        var useCase = new StartNextAuctionUseCase(auctionRepoMock.Object, uowMock.Object, randomUseCase);

        // Act
        await useCase.ExecuteAsync(new StartNextAuctionRequest());

        // Assert
        auctionRepoMock.Verify(x => x.GetLatestAuctionAsync(), Times.Once);
        auctionRepoMock.Verify(x => x.CloseAuctionAndAddNewAsync(It.IsAny<Auction>()), Times.Never);
        auctionRepoMock.Verify(x => x.AddAuction(It.IsAny<Auction>()), Times.Once);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
