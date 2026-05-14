using Application;
using Application.Commands;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
            var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var currentAuction = new Auction()
            {
                Id = 1,
                AppListingId = 1,
                DateAdded = DateTimeOffset.Now.AddDays(-10),
                StartingPrice = 10,
                RowVersion = 1,
            };
            auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
            auctionRepoMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync(currentAuction);
            auctionRepoMock.Setup(x => x.Update(currentAuction, 1));
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var command = new PlaceBidCommand(
                AuctionId: 1,
                Amount: 30,
                UserId: 2,
                RowVersion: 1
            );

            var auctionService = new AuctionService(auctionRepoMock.Object, uowMock.Object, Mock.Of<IAuthService>(), Mock.Of<IAppListingService>(), Mock.Of<IConfiguration>());

            // Act
            await auctionService.PlaceBidAsync(command);

            // Assert
            currentAuction.CurrentPrice.Should().Be(30);
            currentAuction.UserId.Should().Be(2);
            auctionRepoMock.Verify(x => x.GetOpenAuction(1), Times.Once);
            uowMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PlaceBidOnOldAuction_ReturnsNotFoundException_Test()
        {
            // Arrange
            var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var currentAuction = new Auction()
            {
                Id = 2,
                AppListingId = 1,
                DateAdded = DateTimeOffset.Now.AddDays(-10),
                StartingPrice = 10,
                RowVersion = 1,
            };
            auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
            auctionRepoMock.Setup(x => x.GetOpenAuction(2)).ReturnsAsync(currentAuction);
            auctionRepoMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync((Auction?)null);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var command = new PlaceBidCommand(
                AuctionId: 1,
                Amount: 30,
                UserId: 2,
                RowVersion: 1
            );

            var auctionService = new AuctionService(auctionRepoMock.Object, uowMock.Object, Mock.Of<IAuthService>(), Mock.Of<IAppListingService>(), Mock.Of<IConfiguration>());

            // Act & assert
            Func<Task> act = () => auctionService.PlaceBidAsync(command);
            await act.Should().ThrowAsync<NotFoundException>();

            // Assert
            auctionRepoMock.Verify(x => x.GetOpenAuction(1), Times.Once);
            uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task PlaceLowerBid_ReturnsDomainException_Test()
        {
            // Arrange
            var auctionRepoMock = new Mock<IAuctionRepository>(MockBehavior.Strict);
            var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var currentAuction = new Auction()
            {
                Id = 2,
                AppListingId = 1,
                DateAdded = DateTimeOffset.Now.AddDays(-10),
                StartingPrice = 10,
                CurrentPrice = 20,
                RowVersion = 1,
            };
            auctionRepoMock.Setup(x => x.GetLatestAuctionAsync()).ReturnsAsync(currentAuction);
            auctionRepoMock.Setup(x => x.GetOpenAuction(1)).ReturnsAsync(currentAuction);
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            var command = new PlaceBidCommand(
                AuctionId: 1,
                Amount: 15,
                UserId: 2,
                RowVersion: 1
            );

            var auctionService = new AuctionService(auctionRepoMock.Object, uowMock.Object, Mock.Of<IAuthService>(), Mock.Of<IAppListingService>(), Mock.Of<IConfiguration>());

            // Act & assert
            Func<Task> act = () => auctionService.PlaceBidAsync(command);
            await act.Should().ThrowAsync<DomainException>();

            // Assert
            auctionRepoMock.Verify(x => x.GetOpenAuction(1), Times.Once);
            uowMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }
    }
}
