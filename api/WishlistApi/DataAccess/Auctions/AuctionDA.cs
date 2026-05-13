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
        Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction);
        Task<Auction?> GetOpenAuction(int id);
        void SetOriginalRowVersion(Auction auction, uint rowVersion);

    }

    public class AuctionDA(WishlistDbContext context) : IAuctionDA
    {
        public async Task<Auction?> GetLatestAuctionAsync()
        {
            return await context.Auctions.Include(a => a.User).Include(a => a.AppListing).OrderByDescending(x => x.ID).FirstOrDefaultAsync();
        }

        public async Task AddAuctionAsync(Auction auction)
        {
            context.Auctions.Add(auction);
            await context.SaveChangesAsync();
        }

        public async Task<Auction?> GetOpenAuction(int id)
        {
            var latestAuction = await GetLatestAuctionAsync();
            return latestAuction?.ID == id ? latestAuction : null;
        }

        public void SetOriginalRowVersion(Auction auction, uint rowVersion)
        {
            context.Entry(auction)
                .Property(x => x.RowVersion)
                .OriginalValue = rowVersion;
        }

        public async Task CloseAuctionAndAddNewAsync(Auction oldAuction, Auction newAuction)
        {
            oldAuction.Status = AuctionStatus.Closed;
            context.Auctions.Update(oldAuction);            
            context.Auctions.Add(newAuction);
            await context.SaveChangesAsync();
        }

        // Get user auctions (so user can see which auctions he won)
    }
}
