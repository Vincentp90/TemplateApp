using DataAccess.Auctions;
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
    }

    public class AuctionService : IAuctionService
    {
        private readonly IAuctionDA _auctionDA;

        public AuctionService(IAuctionDA auctionDA)
        {
            _auctionDA = auctionDA;
        }

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
    }
}
