using DataAccess.Auctions;
using DataAccess.Helpers;
using DataAccess.Users;
using Domain.Exceptions;
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
        Task PlaceBidAsync(Auction auction);
        Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction);
        Task SimulateBid();
    }

    public class AuctionService(IAuctionDA auctionDA, IAuthService authService, IConfiguration config) : IAuctionService
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

        public async Task PlaceBidAsync(Auction auctionCommand)// TODO PlaceBidCommand command
        {
            var auctionEF = await auctionDA.GetOpenAuction(auctionCommand.ID);

            if (auctionEF == null)
                throw new NotFoundException("Auction not found.");

            var auctionDomain = new Domain.Auction
            {
                CurrentPrice = auctionEF.CurrentPrice,
                StartingPrice = auctionEF.StartingPrice,
                UserId = auctionEF.UserID,
            };

            auctionDomain.PlaceBid(auctionCommand.UserID.Value, auctionCommand.CurrentPrice.Value);

            auctionEF.CurrentPrice = auctionDomain.CurrentPrice;
            auctionEF.UserID = auctionDomain.UserId;

            auctionDA.SetOriginalRowVersion(auctionEF, auctionCommand.RowVersion);

            await ((IUnitOfWork)auctionDA).SaveChangesAsync();// TODO messy solution until we have proper UnitOfWork
            //await _unitOfWork.SaveChangesAsync(); TODO
        }

        public async Task SimulateBid()
        {
            var user = await GetSimulationUser();
            Auction auction = (await GetLatestAuctionAsync())!;
            var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
            await PlaceBidAsync(new Auction { ID = auction.ID, CurrentPrice = newPrice, UserID = user.ID, RowVersion = auction.RowVersion });
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
