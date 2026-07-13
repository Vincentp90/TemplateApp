using Infrastructure.Persistence;
using Infrastructure.Persistence.AppListings;
using Infrastructure.Persistence.Auctions;
using Domain.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;

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

            var existing = new Infrastructure.Persistence.Auctions.Auction
            {
                ID = 1,
                DateAdded = DateTimeOffset.Now,
                CurrentPrice = 150m,
                StartingPrice = 100m,
                Status = Domain.AuctionStatus.Open,
                UserID = null,
                appid = 4,
                RowVersion = 2
            };

            ctx.Auctions.Add(existing);
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

            var bid = await repo.GetLatestAuctionAsync();
            bid.Should().NotBeNull();
            
            // Mutate the domain object using PlaceBid (which sets both CurrentPrice and UserId)
            bid.PlaceBid(bidderUserId: 3, amount: 200m);
            
            repo.Update(bid, bid.RowVersion);
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

            var updated = await ctx.Auctions.FindAsync(new object?[] { 1 }, TestContext.Current.CancellationToken);
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
