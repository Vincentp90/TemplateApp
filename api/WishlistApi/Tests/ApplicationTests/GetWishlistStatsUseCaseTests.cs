using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Domain.Helpers;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class GetWishlistStatsUseCaseTests
{
    [Fact]
    public async Task ReturnsCorrectStats()
    {
        // Arrange
        const int USERID = 1;
        const string OLDESTAPPNAME = "This app is so old";

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
            new List<Domain.WishlistItem>()
            {
                new Domain.WishlistItem(
                    id: 1,
                    appId: 1,
                    name: OLDESTAPPNAME,
                    dateAdded: DateTimeOffset.Now.AddDays(-20),
                    userId: USERID
                ),
                new Domain.WishlistItem(
                    id: 2,
                    appId: 2,
                    name: "A Whole Lot of aaaaaaaa",
                    dateAdded: DateTimeOffset.Now.AddDays(-13),
                    userId: USERID
                ),
                new Domain.WishlistItem(
                    id: 3,
                    appId: 3,
                    name: "MockAppName",
                    dateAdded: DateTimeOffset.Now.AddDays(-10),
                    userId: USERID
                ),
            });

        var useCase = new GetWishlistStatsUseCase(repositoryMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new GetWishlistStatsRequest(USERID));

        // Assert
        result.Should().NotBeNull();
        result.AvgTimeAdded.TotalDays.Should().BeApproximately(14.33, 0.1);
        result.AvgTimeBetweenAdded.Days.Should().Be(5);
        result.OldestItem.Should().Be(OLDESTAPPNAME);
        result.MostCommonCharacter.Should().Be("a");
    }

    [Fact]
    public async Task ReturnsZeroStatsForEmptyWishlist()
    {
        // Arrange
        const int USERID = 1;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
            new List<Domain.WishlistItem>());

        var useCase = new GetWishlistStatsUseCase(repositoryMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new GetWishlistStatsRequest(USERID));

        // Assert
        result.Should().NotBeNull();
        result.AvgTimeAdded.Days.Should().Be(0);
        result.AvgTimeBetweenAdded.Days.Should().Be(0);
        result.OldestItem.Should().Be("");
        result.MostCommonCharacter.Should().Be("");
    }

    [Fact]
    public async Task ReturnsAvgTimeBetweenZeroWithOneItem()
    {
        // Arrange
        const int USERID = 1;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
            new List<Domain.WishlistItem>()
            {
                new Domain.WishlistItem(
                    id: 3,
                    appId: 3,
                    name: "MockAppName",
                    dateAdded: DateTimeOffset.Now.AddDays(-10),
                    userId: USERID
                ),
            });

        var useCase = new GetWishlistStatsUseCase(repositoryMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new GetWishlistStatsRequest(USERID));

        // Assert
        result.Should().NotBeNull();
        result.AvgTimeAdded.Days.Should().Be(10);
        result.AvgTimeBetweenAdded.Days.Should().Be(0);
    }

    [Fact]
    public async Task PicksOneOfTheMostCommonCharacters_WhenTied()
    {
        // Arrange
        const int USERID = 1;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
            new List<Domain.WishlistItem>()
            {
                new Domain.WishlistItem(
                    id: 1,
                    appId: 1,
                    name: "dddd",
                    dateAdded: DateTimeOffset.Now.AddDays(-14),
                    userId: USERID
                ),
                new Domain.WishlistItem(
                    id: 2,
                    appId: 2,
                    name: "bbbb",
                    dateAdded: DateTimeOffset.Now.AddDays(-13),
                    userId: USERID
                ),
                new Domain.WishlistItem(
                    id: 3,
                    appId: 3,
                    name: "ccc",
                    dateAdded: DateTimeOffset.Now.AddDays(-10),
                    userId: USERID
                ),
            });

        var useCase = new GetWishlistStatsUseCase(repositoryMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new GetWishlistStatsRequest(USERID));

        // Assert
        result.MostCommonCharacter.Should().BeOneOf("d", "b");
        result.MostCommonCharacter.Should().NotBe("c");
    }

    [Fact]
    public async Task IgnoresSpaces_WhenCalculatingMostCommonCharacter()
    {
        // Arrange
        const int USERID = 1;

        var repositoryMock = new Mock<IWishlistItemRepository>(MockBehavior.Strict);
        repositoryMock.Setup(x => x.GetWishlistItemsAsync(USERID)).ReturnsAsync(
            new List<Domain.WishlistItem>()
            {
                new Domain.WishlistItem(
                    id: 1,
                    appId: 1,
                    name: "a a c d",
                    dateAdded: DateTimeOffset.Now.AddDays(-14),
                    userId: USERID
                ),
            });

        var useCase = new GetWishlistStatsUseCase(repositoryMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new GetWishlistStatsRequest(USERID));

        // Assert
        result.MostCommonCharacter.Should().Be("a");
        result.MostCommonCharacter.Should().NotBe(" ");
    }
}
