using DataAccess;
using DataAccess.AppListings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WishlistApi.HostedServices;

namespace Tests.ApplicationTests
{
    public class SteamUpdaterServiceTests
    {
        private WishlistDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<WishlistDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new WishlistDbContext(options);
        }

        [Fact]
        public async Task UpdateAppListingsIfEmptyAsync_DoesNothing_WhenAppListingsExist()
        {
            var services = new ServiceCollection();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");
            services.AddSingleton<IConfiguration>(mockConfig.Object);
            var dbContext = CreateDbContext();
            dbContext.AppListings!.Add(new AppListing { appid = 1, name = "Existing Game" });
            dbContext.SaveChanges();
            services.AddSingleton<WishlistDbContext>(dbContext);
            var serviceProvider = services.BuildServiceProvider();

            var mockApiClient = new Mock<ISteamApiClient>();

            var service = new SteamUpdaterService(serviceProvider, mockApiClient.Object);

            await service.UpdateAppListingsIfEmptyAsync(CancellationToken.None);

            mockApiClient.Verify(c => c.GetAppListingsAsync(It.IsAny<string>()), Times.Never());

            var existingListings = await dbContext.AppListings.ToListAsync();
            Assert.Single(existingListings);
            Assert.Equal(1, existingListings[0].appid);
            Assert.Equal("Existing Game", existingListings[0].name);
        }

        [Fact]
        public async Task UpdateAppListingsIfEmptyAsync_SavesAppListings_WhenEmpty()
        {
            var services = new ServiceCollection();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");
            services.AddSingleton<IConfiguration>(mockConfig.Object);
            var dbContext = CreateDbContext();
            services.AddSingleton<WishlistDbContext>(dbContext);
            var serviceProvider = services.BuildServiceProvider();

            var testApps = new List<AppListing>
            {
                new AppListing { appid = 100, name = "Game 1" },
                new AppListing { appid = 200, name = "Game 2" },
            };
            var mockApiClient = new Mock<ISteamApiClient>();
            mockApiClient.Setup(c => c.GetAppListingsAsync(It.IsAny<string>()))
                .ReturnsAsync(new Root
                {
                    response = new AppList { apps = testApps }
                });

            var service = new SteamUpdaterService(serviceProvider, mockApiClient.Object);

            await service.UpdateAppListingsIfEmptyAsync(CancellationToken.None);

            mockApiClient.Verify(c => c.GetAppListingsAsync("test-key"), Times.Once());

            var savedListings = await dbContext.AppListings.OrderBy(a => a.appid).ToListAsync();
            Assert.Equal(2, savedListings.Count);
            Assert.Equal(100, savedListings[0].appid);
            Assert.Equal("Game 1", savedListings[0].name);
            Assert.Equal(200, savedListings[1].appid);
            Assert.Equal("Game 2", savedListings[1].name);
        }

        [Fact]
        public async Task UpdateAppListingsIfEmptyAsync_ThrowsException_WhenApiClientReturnsNull()
        {
            var services = new ServiceCollection();
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");
            services.AddSingleton<IConfiguration>(mockConfig.Object);
            var dbContext = CreateDbContext();
            services.AddSingleton<WishlistDbContext>(dbContext);
            var serviceProvider = services.BuildServiceProvider();

            var mockApiClient = new Mock<ISteamApiClient>();
            mockApiClient.Setup(c => c.GetAppListingsAsync(It.IsAny<string>())).ReturnsAsync((Root?)null);

            var service = new SteamUpdaterService(serviceProvider, mockApiClient.Object);

            var exception = await Assert.ThrowsAsync<Exception>(
                () => service.UpdateAppListingsIfEmptyAsync(CancellationToken.None));
            Assert.Equal("Failed to get game list from steam", exception.Message);

            var listingsAfterException = await dbContext.AppListings.ToListAsync();
            Assert.Empty(listingsAfterException);
        }
    }
}
