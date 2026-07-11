using FluentAssertions;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Exceptions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Tests;

public class DeleteAlertRuleUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_throws_when_rule_not_found()
    {
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((AlertRule?)null);

        var useCase = new DeleteAlertRuleUseCase(alertRuleRepo.Object);

        var act = async () => await useCase.ExecuteAsync("user-1", Guid.NewGuid());

        await act.Should().ThrowAsync<AlertRuleNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_throws_when_rule_belongs_to_different_user()
    {
        var rule = new AlertRule(Guid.NewGuid(), "other-user", new SteamAppId(42), new Money(10m));

        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync(rule);

        var useCase = new DeleteAlertRuleUseCase(alertRuleRepo.Object);

        var act = async () => await useCase.ExecuteAsync("user-1", rule.AlertRuleId);

        await act.Should().ThrowAsync<AlertRuleNotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_deletes_rule_when_found()
    {
        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));

        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync(rule);

        var useCase = new DeleteAlertRuleUseCase(alertRuleRepo.Object);

        await useCase.ExecuteAsync("user-1", rule.AlertRuleId);

        alertRuleRepo.Verify(r => r.DeleteAsync(rule), Times.Once);
    }
}
