using FluentAssertions;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Tests;

public class HandleWishlistItemRemovedUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_deactivates_game_and_rules()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetForUserAsync("user-1")).ReturnsAsync(new[] { rule });

        var useCase = new HandleWishlistItemRemovedUseCase(
            trackedGameRepo.Object,
            alertRuleRepo.Object);

        await useCase.ExecuteAsync("user-1", 42);

        trackedGame.IsActive.Should().BeFalse();
        trackedGameRepo.Verify(r => r.SaveAsync(trackedGame), Times.Once);

        rule.IsActive.Should().BeFalse();
        alertRuleRepo.Verify(r => r.SaveAsync(rule), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_no_op_when_game_not_found()
    {
        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync((TrackedGame?)null);

        var alertRuleRepo = new Mock<IAlertRuleRepository>();

        var useCase = new HandleWishlistItemRemovedUseCase(
            trackedGameRepo.Object,
            alertRuleRepo.Object);

        await useCase.ExecuteAsync("user-1", 42);

        trackedGameRepo.Verify(r => r.SaveAsync(It.IsAny<TrackedGame>()), Times.Never);
        alertRuleRepo.Verify(r => r.SaveAsync(It.IsAny<AlertRule>()), Times.Never);
    }
}
