using Application.UseCases.AppListing;
using Domain;
using Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class GetRandomAppListingUseCaseTests
{
    [Fact]
    public async Task ReturnsRandomAppListing()
    {
        // Arrange
        var expectedListing = new AppListing(42, "Random Game");

        var repoMock = new Mock<IAppListingRepository>(MockBehavior.Strict);
        repoMock.Setup(x => x.GetRandomAsync()).ReturnsAsync(expectedListing);

        var useCase = new GetRandomAppListingUseCase(repoMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new UnitRequest());

        // Assert
        result.Should().Be(expectedListing);
        result.Id.Should().Be(42);
        result.Name.Should().Be("Random Game");
    }
}
