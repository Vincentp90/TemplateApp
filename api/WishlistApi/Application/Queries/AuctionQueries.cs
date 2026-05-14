using Application.Contracts;
using DataAccess;
using DataAccess.Auctions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Queries
{
    public interface IAuctionQueries
    {
        Task<AuctionDto?> GetCurrentAuctionAsync(Guid? currentUserGuid);
    }

    public class AuctionQueries(WishlistDbContext context) : IAuctionQueries
    {
        public async Task<AuctionDto?> GetCurrentAuctionAsync(Guid? currentUserGuid)
        {
            var auction = await context.Auctions.Include(a => a.User).Include(a => a.AppListing).OrderByDescending(x => x.ID).FirstOrDefaultAsync();
            return auction == null ? null : new AuctionDto(
                ID: auction.ID,
                StartDate: auction.DateAdded,
                EndDate: auction.DateAdded + Auction.Duration,
                UserHasBid: currentUserGuid != null && auction.User?.UUID == currentUserGuid,
                StartingPrice: auction.StartingPrice,
                CurrentPrice: auction.CurrentPrice,
                AppID: auction.appid,
                AppName: auction.AppListing.name,
                RowVersion: auction.RowVersion
            );
        }
    }
}
