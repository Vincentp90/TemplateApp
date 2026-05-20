using DataAccess;
using DataAccess.AppListings;
using DataAccess.Auctions;
using Domain.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using WishlistApi.HostedServices;

namespace Tests.DataAccessTests
{
    public class AuctionRepoTest
    {
        [Fact]
        public async Task UpdateAuctionBidAsync_Updates_WhenValid()
        {
            using var ctx = CreateContext();
            var uow = ctx as IUnitOfWork;
            var repo = new AuctionRepository(ctx);

            ctx.AppListings.Add(new AppListing { 
                appid = 4,
                name = "test"
            });
            ctx.SaveChanges();

            var existing = new Domain.Auction(
                id: 1,
                dateAdded: DateTimeOffset.Now,
                currentPrice: 150m,
                startingPrice: 100m,
                status: Domain.AuctionStatus.Open,
                userId: null,
                appListingId: 4,
                rowVersion: 2
            );

            repo.AddAuction(existing);
            await uow.SaveChangesAsync();

            var bid = await repo.GetLatestAuctionAsync();
            bid.Should().NotBeNull();
            // Note: CurrentPrice and UserId are init-only, reassign with constructor for test mutation
            var mutated = new Domain.Auction(
                id: bid.Id,
                dateAdded: bid.DateAdded,
                currentPrice: 200m,
                startingPrice: bid.StartingPrice,
                status: Domain.AuctionStatus.Open,
                userId: 3,
                appListingId: bid.AppListingId,
                rowVersion: bid.RowVersion
            );
            repo.Update(mutated, mutated.RowVersion);
            //repo.Update(bid, bid.RowVersion);
            await uow.SaveChangesAsync();

            var updated = await ctx.Auctions.FindAsync(1);
            updated.Should().NotBeNull();
            updated.CurrentPrice.Should().Be(200m);
            updated.UserID.Should().Be(3);
        }

        private WishlistDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WishlistDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new WishlistDbContext(options);
        }
    }
}