using Application;
using Application.Commands;
using DataAccess.AppListings;
using DataAccess.Auctions;
using DataAccess.Wishlist;
using Domain.Exceptions;
using Domain.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Tests.ApplicationTests
{
    public class AuctionTests
    {        
        [Fact]
        public async Task PlaceBidTest()
        {
            // Arrange
            var auctionDAMock = new Mock<IAuctionDA>(MockBehavior.Strict);
            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var currentAuction = new Auction()
            {
                ID = 1,
                appid = 1,
                AppListing = new AppListing() { appid = 1, name = "MockAppName" },
                DateAdded = DateTimeOffset.Now.AddDays(-10),
                StartingPrice = 10,
                RowVersion = 1,
            };
            auctionDAMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
            auctionDAMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync(currentAuction);
            auctionDAMock.Setup(x => x.SetOriginalRowVersion(It.IsAny<Auction>(), 1));
            uowMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var command = new PlaceBidCommand(
                AuctionId: 1,
                Amount: 30,
                UserId: 2,
                RowVersion: 1
            );

            var auctionService = new AuctionService(auctionDAMock.Object, uowMock.Object, null, null);

            // Act
            await auctionService.PlaceBidAsync(command);

            // Assert
            currentAuction.CurrentPrice.Should().Be(30);
            currentAuction.UserID.Should().Be(2);
            auctionDAMock.Verify(x => x.GetOpenAuction(1), Times.Once);
            auctionDAMock.Verify(x => x.SetOriginalRowVersion(It.IsAny<Auction>(), 1), Times.Once);
            uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PlaceBidOnOldAuction_ReturnsNotFoundException_Test()
        {
            // Arrange
            var auctionDAMock = new Mock<IAuctionDA>(MockBehavior.Strict);
            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var currentAuction = new Auction()
            {
                ID = 2,
                appid = 1,
                AppListing = new AppListing() { appid = 1, name = "MockAppName" },
                DateAdded = DateTimeOffset.Now.AddDays(-10),
                StartingPrice = 10,
                RowVersion = 1,
            };
            auctionDAMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
            auctionDAMock.Setup(x => x.GetOpenAuction(2)).ReturnsAsync(currentAuction);
            auctionDAMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync((Auction?)null);
            auctionDAMock.Setup(x => x.SetOriginalRowVersion(It.IsAny<Auction>(), 1));
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var command = new PlaceBidCommand(
                AuctionId: 1,
                Amount: 30,
                UserId: 2,
                RowVersion: 1
            );

            var auctionService = new AuctionService(auctionDAMock.Object, uowMock.Object, null, null);

            // Act & assert
            Func<Task> act = () => auctionService.PlaceBidAsync(command);
            await act.Should().ThrowAsync<NotFoundException>();

            // Assert
            auctionDAMock.Verify(x => x.GetOpenAuction(1), Times.Once);
            auctionDAMock.Verify(x => x.SetOriginalRowVersion(It.IsAny<Auction>(), 1), Times.Never);
            uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task PlaceLowerBid_ReturnsDomainException_Test()
        {
            // Arrange
            var auctionDAMock = new Mock<IAuctionDA>(MockBehavior.Strict);
            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var currentAuction = new Auction()
            {
                ID = 2,
                appid = 1,
                AppListing = new AppListing() { appid = 1, name = "MockAppName" },
                DateAdded = DateTimeOffset.Now.AddDays(-10),
                StartingPrice = 10,
                CurrentPrice = 20,
                RowVersion = 1,
            };
            auctionDAMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
            auctionDAMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync(currentAuction);
            auctionDAMock.Setup(x => x.SetOriginalRowVersion(It.IsAny<Auction>(), 1));
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var command = new PlaceBidCommand(
                AuctionId: 1,
                Amount: 15,
                UserId: 2,
                RowVersion: 1
            );

            var auctionService = new AuctionService(auctionDAMock.Object, uowMock.Object, null, null);

            // Act & assert
            Func<Task> act = () => auctionService.PlaceBidAsync(command);
            await act.Should().ThrowAsync<DomainException>();

            // Assert
            auctionDAMock.Verify(x => x.GetOpenAuction(1), Times.Once);
            auctionDAMock.Verify(x => x.SetOriginalRowVersion(It.IsAny<Auction>(), 1), Times.Never);
            uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }
    }
}
