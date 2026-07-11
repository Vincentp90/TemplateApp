using Application.UseCases.Auction;
using Application.UseCases.Auction.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class PlaceBidUseCaseTests
{
    [Fact]
    public async Task PlacesBidSuccessfully()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentAuction = new Domain.Auction(
            dateAdded: DateTimeOffset.UtcNow,
            startingPrice: 10.0m,
            appListingId: 1
        );
        auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
        auctionRepoMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync(currentAuction);
        auctionRepoMock.Setup(x => x.Update(currentAuction, 1));
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var useCase = new PlaceBidUseCase(auctionRepoMock.Object, uowMock.Object);
        var request = new PlaceBidRequest(
            AuctionId: 1,
            Amount: 30,
            UserId: 2,
            RowVersion: 1
        );

        // Act
        await useCase.ExecuteAsync(request);

        // Assert
        currentAuction.CurrentPrice.Should().Be(30);
        currentAuction.UserId.Should().Be(2);
        auctionRepoMock.Verify(x => x.GetOpenAuction(1), Times.Once);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ThrowsNotFoundException_WhenAuctionNotFound()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentAuction = Domain.Auction.FromData(
            id: 2,
            dateAdded: DateTimeOffset.Now.AddDays(-10),
            currentPrice: null,
            startingPrice: 10,
            status: AuctionStatus.Open,
            userId: null,
            appListingId: 1,
            rowVersion: 0
        );
        auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
        auctionRepoMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync((Domain.Auction?)null);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var useCase = new PlaceBidUseCase(auctionRepoMock.Object, uowMock.Object);
        var request = new PlaceBidRequest(
            AuctionId: 1,
            Amount: 30,
            UserId: 2,
            RowVersion: 1
        );

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync(request);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Auction not found.");

        auctionRepoMock.Verify(x => x.GetOpenAuction(1), Times.Once);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ThrowsDomainException_WhenBidLowerThanCurrentPrice()
    {
        // Arrange
        var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentAuction = Domain.Auction.FromData(
            id: 2,
            dateAdded: DateTimeOffset.Now.AddDays(-10),
            currentPrice: 20,
            startingPrice: 10,
            status: AuctionStatus.Open,
            userId: null,
            appListingId: 1,
            rowVersion: 0
        );
        auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
        auctionRepoMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync(currentAuction);
        uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var useCase = new PlaceBidUseCase(auctionRepoMock.Object, uowMock.Object);
        var request = new PlaceBidRequest(
            AuctionId: 1,
            Amount: 15,
            UserId: 2,
            RowVersion: 1
        );

        // Act & assert
        Func<Task> act = () => useCase.ExecuteAsync(request);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Bid must be higher than current price.");

        auctionRepoMock.Verify(x => x.GetOpenAuction(1), Times.Once);
        uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
