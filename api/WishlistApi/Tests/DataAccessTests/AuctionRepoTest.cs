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

            var existing = new Domain.Auction
            {
                StartingPrice = 100,
                CurrentPrice = 150,
                RowVersion = 2,
                AppListingId = 4
            };

            repo.AddAuction(existing);
            await uow.SaveChangesAsync();

            var bid = await repo.GetLatestAuctionAsync();
            bid.Should().NotBeNull();
            bid.CurrentPrice = 200;
            bid.UserId = 3;
            repo.Update(bid, bid.RowVersion);
            await uow.SaveChangesAsync();

            var updated = await ctx.Auctions.FindAsync(1);
            updated.Should().NotBeNull();
            updated.CurrentPrice.Should().Be(200);
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
