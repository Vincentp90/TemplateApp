using Infrastructure.Persistence.Migrations;
using Domain;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Infrastructure.Persistence.Auctions
{
    public class AuctionRepository(WishlistDbContext context) : IAuctionRepository
    {
        public async Task<Domain.Auction?> GetOpenAuction(int id)
        {
            var latestAuction = await GetLatestAuctionAsync();
            if(latestAuction == null || latestAuction.Id != id)
                return null;

            return latestAuction;
        }

        /// <summary>
        /// Local update of the auction entity, it does not call SaveChanges
        /// </summary>
        /// <param name="auction">Auction domain object</param>
        /// <param name="rowVersion"></param>
        public void Update(Domain.Auction auction, uint rowVersion)
        {
            var entity = context.Auctions.Single(x => x.ID == auction.Id);

            entity.CurrentPrice = auction.CurrentPrice;
            entity.UserID = auction.UserId;

            context.Entry(entity).Property(x => x.RowVersion).OriginalValue = rowVersion;
        }

        public async Task<Domain.Auction?> GetLatestAuctionAsync()
        {
            var entity = await context.Auctions
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            return MapToDomain(entity);
        }

        private static Domain.Auction MapToDomain(Infrastructure.Persistence.Auctions.Auction entity)
        {
            return Domain.Auction.FromData(
                id: entity.ID,
                dateAdded: entity.DateAdded,
                currentPrice: entity.CurrentPrice,
                startingPrice: entity.StartingPrice,
                status: entity.Status,
                userId: entity.UserID,
                appListingId: entity.appid,
                rowVersion: entity.RowVersion
            );
        }

        public void AddAuction(Domain.Auction auction)
        {
            var entity = new Auction
            {
                DateAdded = auction.DateAdded,
                Status = auction.Status,
                StartingPrice = auction.StartingPrice,
                appid = auction.AppListingId,
            };
            context.Auctions.Add(entity);
        }

        public async Task CloseAuctionAndAddNewAsync(Domain.Auction newAuction)
        {
            var entity = new Auction
            {
                DateAdded = newAuction.DateAdded,
                Status = newAuction.Status,
                StartingPrice = newAuction.StartingPrice,
                appid = newAuction.AppListingId,
            };

            var oldAuction = await context.Auctions.OrderByDescending(x => x.ID).FirstAsync();
            oldAuction.Status = AuctionStatus.Closed;

            context.Auctions.Add(entity);
        }
    }
}
