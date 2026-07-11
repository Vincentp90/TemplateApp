using Application;
using Application.Contracts;
using Application.Queries;
using Application.UseCases.User;
using Application.UseCases.User.Requests;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class GetPaginatedUsersUseCaseTests
{
    [Fact]
    public async Task ReturnsPaginatedUsers()
    {
        // Arrange
        var readModelMock = new Mock<IUserReadModel>(MockBehavior.Strict);
        var expectedUsers = new List<UserSummaryDto>
        {
            new UserSummaryDto(Guid.NewGuid(), "user1"),
            new UserSummaryDto(Guid.NewGuid(), "user2"),
        };
        readModelMock.Setup(x => x.GetUsersAsync(1, 10)).ReturnsAsync(expectedUsers);

        var useCase = new GetPaginatedUsersUseCase(readModelMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(new GetPaginatedUsersRequest(1, 10));

        // Assert
        result.Should().HaveCount(2);
        result[0].Username.Should().Be("user1");
        result[1].Username.Should().Be("user2");
    }
}
