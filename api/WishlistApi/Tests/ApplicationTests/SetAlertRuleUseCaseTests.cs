using Application;
using Application.UseCases.Wishlist;
using Application.UseCases.Wishlist.Requests;
using Application.Contracts;
using FluentAssertions;
using Moq;

namespace Tests.ApplicationTests;

public class SetAlertRuleUseCaseTests
{
    [Fact]
    public async Task DelegatesToAlertProxy()
    {
        // Arrange
        const string userId = "123";
        const int appId = 42;
        const decimal threshold = 19.99m;
        const string currency = "USD";

        var alertProxyMock = new Mock<ISteamTrackerAlertProxy>(MockBehavior.Strict);
        alertProxyMock.Setup(x => x.SetAlertRuleAsync(userId, appId, threshold, currency)).Returns(Task.CompletedTask);
        var useCase = new SetAlertRuleUseCase(alertProxyMock.Object);

        // Act
        await useCase.ExecuteAsync(new SetAlertRuleRequest(userId, appId, threshold, currency));

        // Assert
        alertProxyMock.Verify(p => p.SetAlertRuleAsync(userId, appId, threshold, currency), Times.Once);
    }
}
