using Application.Commands;
using DataAccess.Auctions;
using DataAccess.Users;
using Domain.Exceptions;
using Domain.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Application
{
    public interface IAuctionService
    {
        Task<Auction?> GetLatestAuctionAsync();
        Task AddAuctionAsync(Auction auction);
        Task PlaceBidAsync(PlaceBidCommand command);
        Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction);
        Task SimulateBid();
    }

    public class AuctionService(IAuctionDA auctionDA, IUnitOfWork unitOfWork,IAuthService authService, IConfiguration config) : IAuctionService
    {
        public async Task AddAuctionAsync(Auction auction)
        {
            await auctionDA.AddAuctionAsync(auction);
        }

        public async Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction)
        {
            await auctionDA.CloseAuctionAndAddNewAsync(oldAuction, newAuction);
        }

        public async Task<Auction?> GetLatestAuctionAsync()
        {
            return await auctionDA.GetLatestAuctionAsync();
        }

        public async Task PlaceBidAsync(PlaceBidCommand command)
        {
            var auctionEF = await auctionDA.GetOpenAuction(command.AuctionId);

            if (auctionEF == null)
                throw new NotFoundException("Auction not found.");

            // TODO implement repository so we don't need this mapping here
            var auctionDomain = new Domain.Auction
            {
                CurrentPrice = auctionEF.CurrentPrice,
                StartingPrice = auctionEF.StartingPrice,
                UserId = auctionEF.UserID,
            };

            auctionDomain.PlaceBid(command.UserId, command.Amount);

            auctionEF.CurrentPrice = auctionDomain.CurrentPrice;
            auctionEF.UserID = auctionDomain.UserId;

            auctionDA.SetOriginalRowVersion(auctionEF, command.RowVersion);

            await unitOfWork.SaveChangesAsync();
        }

        public async Task SimulateBid()
        {
            var user = await GetSimulationUser();
            Auction auction = (await GetLatestAuctionAsync())!;
            var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
            await PlaceBidAsync(new PlaceBidCommand(AuctionId: auction.ID, Amount: newPrice, UserId: user.ID, RowVersion: auction.RowVersion ));
        }

        private async Task<User> GetSimulationUser()
        {
            const string username = "SimulateAuctionUser";
            string password = config["SimUserPassword"]!;
            var user = await authService.LoginAsync(username, password);
            if (user == null)
            {
                await authService.AddUserAsync(username, password);
                user = await authService.LoginAsync(username, password);
            }
            return user!;
        }
    }
}
