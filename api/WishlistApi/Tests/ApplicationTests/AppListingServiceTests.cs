using Application;
using Domain.Repositories;
using Domain;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Tests.ApplicationTests
{
    public class AppListingServiceTests
    {
        [Fact]
        public async Task EnsureAppListingsPopulatedAsync_DoesNothing_WhenAppListingsExist()
        {
            var mockRepo = new Mock<IAppListingRepository>();
            mockRepo.Setup(r => r.HasAnyAsync()).ReturnsAsync(true);

            var mockApiClient = new Mock<ISteamApiClient>();
            var mockConfig = new Mock<IConfiguration>();

            var service = new AppListingService(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

            await service.EnsureAppListingsPopulatedAsync();

            mockRepo.Verify(r => r.HasAnyAsync(), Times.Once());
            mockApiClient.Verify(c => c.GetAppListingsAsync(It.IsAny<string>()), Times.Never());
            mockRepo.Verify(r => r.SaveAsync(It.IsAny<IEnumerable<Domain.AppListing>>()), Times.Never());
        }

        [Fact]
        public async Task EnsureAppListingsPopulatedAsync_SavesAppListings_WhenEmpty()
        {
            var mockRepo = new Mock<IAppListingRepository>();
            mockRepo.Setup(r => r.HasAnyAsync()).ReturnsAsync(false);

            var testApps = new List<SteamAppEntry>
            {
                new SteamAppEntry(100, "Game 1"),
                new SteamAppEntry(200, "Game 2"),
            };
            var mockApiClient = new Mock<ISteamApiClient>();
            mockApiClient.Setup(c => c.GetAppListingsAsync(It.IsAny<string>()))
                .ReturnsAsync(new SteamAppList(testApps));

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");

            var service = new AppListingService(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

            await service.EnsureAppListingsPopulatedAsync();

            mockApiClient.Verify(c => c.GetAppListingsAsync("test-key"), Times.Once());
            mockRepo.Verify(r => r.SaveAsync(It.IsAny<IEnumerable<Domain.AppListing>>()), Times.Once());
        }

        [Fact]
        public async Task EnsureAppListingsPopulatedAsync_DeduplicatesApps()
        {
            var mockRepo = new Mock<IAppListingRepository>();
            mockRepo.Setup(r => r.HasAnyAsync()).ReturnsAsync(false);

            var duplicateApps = new List<SteamAppEntry>
            {
                new SteamAppEntry(100, "Game 1"),
                new SteamAppEntry(100, "Game 1"),
                new SteamAppEntry(200, "Game 2"),
            };
            var mockApiClient = new Mock<ISteamApiClient>();
            mockApiClient.Setup(c => c.GetAppListingsAsync(It.IsAny<string>()))
                .ReturnsAsync(new SteamAppList(duplicateApps));

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");

            var service = new AppListingService(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

            await service.EnsureAppListingsPopulatedAsync();

            mockRepo.Verify(r => r.SaveAsync(It.IsAny<IEnumerable<Domain.AppListing>>()), Times.Once());
        }

        [Fact]
        public async Task EnsureAppListingsPopulatedAsync_ThrowsException_WhenApiClientReturnsNull()
        {
            var mockRepo = new Mock<IAppListingRepository>();
            mockRepo.Setup(r => r.HasAnyAsync()).ReturnsAsync(false);

            var mockApiClient = new Mock<ISteamApiClient>();
            mockApiClient.Setup(c => c.GetAppListingsAsync(It.IsAny<string>())).ReturnsAsync((SteamAppList?)null);

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");

            var service = new AppListingService(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

            var exception = await Assert.ThrowsAsync<Exception>(
                () => service.EnsureAppListingsPopulatedAsync());
            Assert.Equal("Failed to get game list from steam", exception.Message);
        }
    }
}
