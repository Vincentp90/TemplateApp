using Application.Contracts;
using Application.Queries;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ReadAdapters;

public class AuctionReadAdapter(WishlistDbContext context) : IAuctionReadModel
{
    public async Task<AuctionDto?> GetCurrentAuctionAsync(Guid? currentUserGuid)
    {
        var auction = await context.Auctions
            .Include(a => a.User)
            .Include(a => a.AppListing)
            .OrderByDescending(x => x.ID)
            .FirstOrDefaultAsync();

        return auction == null ? null : new AuctionDto(
            ID: auction.ID,
            StartDate: auction.DateAdded,
            EndDate: auction.DateAdded + Domain.Auction.Duration,
            UserHasBid: currentUserGuid != null && auction.User?.UUID == currentUserGuid,
            StartingPrice: auction.StartingPrice,
            CurrentPrice: auction.CurrentPrice,
            AppID: auction.appid,
            AppName: auction.AppListing!.name,
            RowVersion: auction.RowVersion
        );
    }
}
