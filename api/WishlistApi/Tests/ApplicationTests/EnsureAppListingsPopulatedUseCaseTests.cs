using Application.UseCases.AppListing;
using Domain;
using Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Tests.ApplicationTests;

public class EnsureAppListingsPopulatedUseCaseTests
{
    [Fact]
    public async Task DoesNothing_WhenAppListingsExist()
    {
        // Arrange
        var mockRepo = new Mock<IAppListingRepository>();
        mockRepo.Setup(r => r.HasAnyAsync()).ReturnsAsync(true);

        var mockApiClient = new Mock<ISteamApiClient>();
        var mockConfig = new Mock<IConfiguration>();

        var useCase = new EnsureAppListingsPopulatedUseCase(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

        // Act
        await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        mockRepo.Verify(r => r.HasAnyAsync(), Times.Once());
        mockApiClient.Verify(c => c.GetAppListingsAsync(It.IsAny<string>()), Times.Never());
        mockRepo.Verify(r => r.SaveAsync(It.IsAny<IEnumerable<AppListing>>()), Times.Never());
    }

    [Fact]
    public async Task SavesAppListings_WhenEmpty()
    {
        // Arrange
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

        var useCase = new EnsureAppListingsPopulatedUseCase(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

        // Act
        await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        mockApiClient.Verify(c => c.GetAppListingsAsync("test-key"), Times.Once());
        mockRepo.Verify(r => r.SaveAsync(It.IsAny<IEnumerable<AppListing>>()), Times.Once());
    }

    [Fact]
    public async Task DeduplicatesApps()
    {
        // Arrange
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

        var useCase = new EnsureAppListingsPopulatedUseCase(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

        // Act
        await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        mockRepo.Verify(r => r.SaveAsync(It.IsAny<IEnumerable<AppListing>>()), Times.Once());
    }

    [Fact]
    public async Task ThrowsException_WhenApiClientReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<IAppListingRepository>();
        mockRepo.Setup(r => r.HasAnyAsync()).ReturnsAsync(false);

        var mockApiClient = new Mock<ISteamApiClient>();
        mockApiClient.Setup(c => c.GetAppListingsAsync(It.IsAny<string>())).ReturnsAsync((SteamAppList?)null);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["SteamAPIKEY"]).Returns("test-key");

        var useCase = new EnsureAppListingsPopulatedUseCase(mockRepo.Object, mockApiClient.Object, mockConfig.Object);

        // Act & assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => useCase.ExecuteAsync(TestContext.Current.CancellationToken));
        exception.Message.Should().Be("Failed to get game list from steam");
    }
}
