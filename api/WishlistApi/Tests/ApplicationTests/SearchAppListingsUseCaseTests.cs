using Application;
using Application.UseCases.AppListing;
using Application.UseCases.AppListing.Requests;
using Domain;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class SearchAppListingsUseCaseTests
{
    [Fact]
    public async Task ReturnsEmpty_WhenTermTooShort()
    {
        // Arrange
        var useCase = new SearchAppListingsUseCase(Mock.Of<IAppListingRepository>());

        // Act
        var result = await useCase.ExecuteAsync(new SearchAppListingsRequest("ab"));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenTermEmpty()
    {
        // Arrange
        var useCase = new SearchAppListingsUseCase(Mock.Of<IAppListingRepository>());

        // Act
        var result = await useCase.ExecuteAsync(new SearchAppListingsRequest(""));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsDtos_WhenTermValid()
    {
        // Arrange
        var listings = new List<AppListing>
        {
            new AppListing(100, "Elden Ring"),
            new AppListing(200, "Elden Ring: Shadow of the Erdtree"),
        };

        var repoMock = new Mock<IAppListingRepository>(MockBehavior.Strict);
        repoMock.Setup(x => x.SearchAsync("elden")).ReturnsAsync(listings);

        var useCase = new SearchAppListingsUseCase(repoMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new SearchAppListingsRequest("elden"));

        // Assert
        result.Should().HaveCount(2);
        result[0].appid.Should().Be(100);
        result[0].name.Should().Be("Elden Ring");
        result[1].appid.Should().Be(200);
        result[1].name.Should().Be("Elden Ring: Shadow of the Erdtree");
    }
}
