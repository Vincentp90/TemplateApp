using FluentAssertions;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Exceptions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Tests;

public class SetAlertRuleUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_throws_when_game_not_tracked()
    {
        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync((TrackedGame?)null);

        var useCase = new SetAlertRuleUseCase(
            trackedGameRepo.Object,
            new Mock<IAlertRuleRepository>().Object);

        var act = async () => await useCase.ExecuteAsync("user-1", 42, 10m);

        await act.Should().ThrowAsync<TrackingNotFoundException>()
            .WithMessage("No active tracking for AppId 42.");
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_game_inactive()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        trackedGame.StopTracking();

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var useCase = new SetAlertRuleUseCase(
            trackedGameRepo.Object,
            new Mock<IAlertRuleRepository>().Object);

        var act = async () => await useCase.ExecuteAsync("user-1", 42, 10m);

        await act.Should().ThrowAsync<TrackingNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_saves_alert_rule_when_game_active()
    {
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);

        var trackedGameRepo = new Mock<ITrackedGameRepository>();
        trackedGameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(trackedGame);

        var alertRuleRepo = new Mock<IAlertRuleRepository>();

        var useCase = new SetAlertRuleUseCase(
            trackedGameRepo.Object,
            alertRuleRepo.Object);

        await useCase.ExecuteAsync("user-1", 42, 10m);

        alertRuleRepo.Verify(r => r.SaveAsync(It.Is<AlertRule>(ar =>
            ar.UserId == "user-1" &&
            ar.AppId.Value == 42 &&
            ar.TriggerBelowPrice.Amount == 10m)), Times.Once);
    }
}
