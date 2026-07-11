using Application;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Application.Contracts;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class DeleteAlertRuleUseCaseTests
{
    [Fact]
    public async Task DelegatesToAlertProxy()
    {
        // Arrange
        const string userId = "123";
        var alertRuleId = Guid.NewGuid();

        var alertProxyMock = new Mock<ISteamTrackerAlertProxy>(MockBehavior.Strict);
        alertProxyMock.Setup(x => x.DeleteAlertRuleAsync(userId, alertRuleId)).Returns(Task.CompletedTask);
        var useCase = new DeleteAlertRuleUseCase(alertProxyMock.Object);

        // Act
        await useCase.ExecuteAsync(new DeleteAlertRuleRequest(userId, alertRuleId));

        // Assert
        alertProxyMock.Verify(p => p.DeleteAlertRuleAsync(userId, alertRuleId), Times.Once);
    }
}
