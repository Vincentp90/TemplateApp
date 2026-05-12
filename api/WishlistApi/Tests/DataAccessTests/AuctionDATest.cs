using DataAccess;
using DataAccess.AppListings;
using DataAccess.Auctions;
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
    public class AuctionDATest
    {
        [Fact]
        public async Task UpdateAuctionBidAsync_Updates_WhenValid()
        {
            using var ctx = CreateContext();
            var da = new AuctionDA(ctx);

            ctx.AppListings.Add(new AppListing { 
                appid = 4,
                name = "test"
            });
            ctx.SaveChanges();

            var existing = new Auction
            {
                StartingPrice = 100,
                CurrentPrice = 150,
                RowVersion = 2,
                appid = 4
            };

            await da.AddAuctionAsync(existing);

            var bid = await da.GetLatestAuctionAsync();
            bid.Should().NotBeNull();
            bid.CurrentPrice = 200;
            bid.UserID = 3;

            await da.SaveChangesAsync();

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
