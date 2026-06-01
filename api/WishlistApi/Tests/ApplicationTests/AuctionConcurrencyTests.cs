using Application;
using Application.Commands;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Auctions;
using Domain;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Testcontainers.PostgreSql;

namespace Tests.ApplicationTests
{
    public class AuctionConcurrencyTests : IAsyncLifetime
    {
        readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18.1")
            .WithDatabase("concurrency_test")
            .WithUsername("user")
            .WithPassword("pass")
            .Build();

        public async Task InitializeAsync()
        {
            await _db.StartAsync();
            var ctx = CreateContext();
            await ctx.Database.EnsureCreatedAsync();
            await ctx.DisposeAsync();
        }
        public async Task DisposeAsync() => await _db.DisposeAsync();

        WishlistDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<WishlistDbContext>()
                .UseNpgsql(_db.GetConnectionString())
                .Options;
            return new WishlistDbContext(options);
        }

        void Seed(WishlistDbContext ctx)
        {
            ctx.Users.Add(new Infrastructure.Persistence.Users.User { ID = 1, Username = "bidder1" });
            ctx.Users.Add(new Infrastructure.Persistence.Users.User { ID = 2, Username = "bidder2" });
            ctx.AppListings.Add(new Infrastructure.Persistence.AppListings.AppListing { appid = 1, name = "Test App" });
            ctx.Auctions.Add(new Infrastructure.Persistence.Auctions.Auction
            {
                DateAdded = DateTimeOffset.UtcNow,
                StartingPrice = 10m,
                Status = AuctionStatus.Open,
                appid = 1
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task PlaceBid_Concurrent_SameRowVersion_OneSucceedsOneFails()
        {
            // Arrange - seed in a dedicated context
            using var seedCtx = CreateContext();
            Seed(seedCtx);
            await seedCtx.DisposeAsync();

            // Each bidder gets its own DbContext to simulate real concurrent connections
            var ctx1 = CreateContext();
            var ctx2 = CreateContext();

            var repo1 = new AuctionRepository(ctx1);
            var service1 = new AuctionService(
                repo1, ctx1,
                Mock.Of<IAuthService>(),
                Mock.Of<IAppListingService>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<IUserService>());

            var auction = await repo1.GetLatestAuctionAsync();
            var rowVersion = auction!.RowVersion;

            var command1 = new PlaceBidCommand(AuctionId: auction.Id, Amount: 30m, UserId: 1, RowVersion: rowVersion);
            var command2 = new PlaceBidCommand(AuctionId: auction.Id, Amount: 35m, UserId: 2, RowVersion: rowVersion);

            // Act - fire both bids concurrently
            var results = await Task.WhenAll(
                TryPlaceBidAsync(service1, command1),
                TryPlaceBidAsync(
                    new AuctionService(new AuctionRepository(ctx2), ctx2,
                        Mock.Of<IAuthService>(), Mock.Of<IAppListingService>(),
                        Mock.Of<IConfiguration>(), Mock.Of<IUserService>()),
                    command2));

            // Assert
            var successes = results.Count(r => r.Success);
            var failures = results.Count(r => !r.Success);

            successes.Should().Be(1, "exactly one bid should succeed");
            failures.Should().Be(1, "exactly one bid should fail");

            var failedResult = results.First(r => !r.Success);
            failedResult.Exception.Should().BeAssignableTo<DbUpdateException>();

            await ctx1.DisposeAsync();
            await ctx2.DisposeAsync();
        }

        [Fact]
        public async Task PlaceBid_Sequential_SameRowVersion_SecondFails()
        {
            // Arrange
            var ctx = CreateContext();
            Seed(ctx);

            var repo = new AuctionRepository(ctx);
            var service = new AuctionService(
                repo, ctx,
                Mock.Of<IAuthService>(),
                Mock.Of<IAppListingService>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<IUserService>());

            // Synchronously get the current auction state
            var auction = await repo.GetLatestAuctionAsync();
            var rowVersion = auction!.RowVersion;

            var command1 = new PlaceBidCommand(AuctionId: auction.Id, Amount: 30m, UserId: 1, RowVersion: rowVersion);
            var command2 = new PlaceBidCommand(AuctionId: auction.Id, Amount: 35m, UserId: 2, RowVersion: rowVersion);

            // Act - user 1 bids first (succeeds)
            var act1 = async () => await service.PlaceBidAsync(command1);
            await act1.Should().NotThrowAsync("user 1's bid should succeed");

            // Act - user 2 bids with stale RowVersion (fails)
            var act2 = async () => await service.PlaceBidAsync(command2);
            await act2.Should().ThrowAsync<DbUpdateConcurrencyException>("user 2's bid should fail due to stale RowVersion");

            await ctx.DisposeAsync();
        }

        static async Task<(bool Success, Exception? Exception)> TryPlaceBidAsync(AuctionService service, PlaceBidCommand command)
        {
            try
            {
                await service.PlaceBidAsync(command);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex);
            }
        }
    }
}
