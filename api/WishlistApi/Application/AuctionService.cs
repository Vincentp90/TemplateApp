using DataAccess.Auctions;
using DataAccess.Users;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IAuctionService
    {
        Task<Auction?> GetLatestAuctionAsync();
        Task AddAuctionAsync(Auction auction);
        Task UpdateAuctionBidAsync(Auction auction);
        Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction);
        Task SimulateBid();
    }

    public class AuctionService(IAuctionDA _auctionDA, IUserService _userService, IConfiguration _config) : IAuctionService
    {
        public async Task AddAuctionAsync(Auction auction)
        {
            await _auctionDA.AddAuctionAsync(auction);
        }

        public async Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction)
        {
            await _auctionDA.CloseAuctionAndAddNewAsync(oldAuction, newAuction);
        }

        public async Task<Auction?> GetLatestAuctionAsync()
        {
            return await _auctionDA.GetLatestAuctionAsync();
        }

        public async Task UpdateAuctionBidAsync(Auction auction)
        {
            await _auctionDA.UpdateAuctionBidAsync(auction);
        }

        public async Task SimulateBid()
        {
            var user = await GetSimulationUser();
            Auction auction = (await GetLatestAuctionAsync())!;
            var newPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
            auction.CurrentPrice = (auction.CurrentPrice ?? auction.StartingPrice) + 10.0M;
            auction.UserID = user.ID;
            await UpdateAuctionBidAsync(auction);
        }

        private async Task<User> GetSimulationUser()
        {
            const string username = "SimulateAuctionUser";
            string password = _config["SimUserPassword"]!;
            var user = await _userService.LoginUserAsync(username, password);
            if (user == null)
            {
                await _userService.AddUserAsync(username, password);
                user = await _userService.LoginUserAsync(username, password);
            }
            return user!;
        }
    }
}
