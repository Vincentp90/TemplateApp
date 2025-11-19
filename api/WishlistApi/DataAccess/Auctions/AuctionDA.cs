using DataAccess.Wishlist;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Auctions
{
    public interface IAuctionDA
    {
        Task<Auction?> GetLatestAuctionAsync();
        Task AddAuctionAsync(Auction auction);
        Task UpdateAuctionBidAsync(Auction auction);
        Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction);
    }

    public class AuctionDA : IAuctionDA
    {
        private readonly WishlistDbContext _context;

        public AuctionDA(WishlistDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<Auction?> GetLatestAuctionAsync()
        {
            return await _context.Auctions.Include(a => a.User).Include(a => a.AppListing).OrderByDescending(x => x.ID).FirstOrDefaultAsync();
        }

        public async Task AddAuctionAsync(Auction auction)
        {
            _context.Auctions.Add(auction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAuctionBidAsync(Auction auctionBid)
        {
            var auction = await GetLatestAuctionAsync();
            if(auction == null)
                throw new DbUpdateConcurrencyException("No open auction found.");
            if(auction.ID != auctionBid.ID)
                throw new DbUpdateConcurrencyException("Auction is no longer open.");

            if(auction.StartingPrice >= auctionBid.CurrentPrice)
                throw new DbUpdateConcurrencyException("Bid is not higher than starting price.");// TODO give more appropriate error type
            // If detached (different object), check new price is higher
            if (auctionBid != auction && auction.CurrentPrice >= auctionBid.CurrentPrice)
                throw new DbUpdateConcurrencyException("Bid is not higher than current price.");
            

            auction.CurrentPrice = auctionBid.CurrentPrice;
            auction.UserID = auctionBid.UserID;

            // Setting RowVersion to make EF check for optimistic concurrency
            _context.Entry(auction).Property(a => a.RowVersion).OriginalValue = auctionBid.RowVersion;

            _context.Auctions.Update(auction);
            await _context.SaveChangesAsync();
        }

        public async Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction)
        {
            oldAuction.Status = AuctionStatus.Closed;
            _context.Auctions.Update(oldAuction);            
            _context.Auctions.Add(newAuction);
            await _context.SaveChangesAsync();
        }

        // Get user auctions (so user can see which auctions he won)
    }
}
