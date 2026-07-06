using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Moq;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Services;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Tests;

public class ProcessPriceCheckUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_saves_price_and_name()
    {
        var gameRepo = new Mock<IGameRepository>();
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>())).ReturnsAsync(Array.Empty<AlertRule>());
        var notificationPublisher = new Mock<INotificationPublisher>();
        var evaluator = new PriceAlertEvaluator();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["RabbitMQ:AlertsEnabled"]).Returns("true");

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo.Object,
            alertRuleRepo.Object,
            notificationPublisher.Object,
            evaluator,
            config.Object);

        await useCase.ExecuteAsync(42, new Money(9.99m), "Test Game");

        gameRepo.Verify(r => r.SaveAsync(It.Is<Game>(g =>
            g.AppId.Value == 42 &&
            g.Name == "Test Game" &&
            g.CurrentPrice != null && g.CurrentPrice.Value.Amount == 9.99m)), Times.Once);
    }

    [Fact]
    public async Task Execute_async_evaluates_alert_rules()
    {
        var game = new Game(new SteamAppId(42));
        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>())).ReturnsAsync(new[] { rule });

        var notificationPublisher = new Mock<INotificationPublisher>();
        var evaluator = new PriceAlertEvaluator();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["RabbitMQ:AlertsEnabled"]).Returns("true");

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo.Object,
            alertRuleRepo.Object,
            notificationPublisher.Object,
            evaluator,
            config.Object);

        // Price is below threshold, so alert should trigger
        await useCase.ExecuteAsync(42, new Money(5m), "Test Game");

        notificationPublisher.Verify(
            n => n.PublishAsync(rule.AlertRuleId, "user-1", 42, 5m, "EUR"),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_does_not_notify_when_no_alert_triggers()
    {
        var game = new Game(new SteamAppId(42));
        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(3m));
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>())).ReturnsAsync(new[] { rule });

        var notificationPublisher = new Mock<INotificationPublisher>();
        var evaluator = new PriceAlertEvaluator();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["RabbitMQ:AlertsEnabled"]).Returns("true");

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo.Object,
            alertRuleRepo.Object,
            notificationPublisher.Object,
            evaluator,
            config.Object);

        // Price is above threshold, no alert
        await useCase.ExecuteAsync(42, new Money(10m), "Test Game");

        notificationPublisher.Verify(
            n => n.PublishAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_alertsDisabled_skipsRuleEvaluationAndPublishing()
    {
        var game = new Game(new SteamAppId(42));
        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>())).ReturnsAsync(new[] { rule });

        var notificationPublisher = new Mock<INotificationPublisher>();
        var evaluator = new PriceAlertEvaluator();

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["RabbitMQ:AlertsEnabled"]).Returns("false");

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo.Object,
            alertRuleRepo.Object,
            notificationPublisher.Object,
            evaluator,
            config.Object);

        // Price is below threshold, but alerts are disabled
        await useCase.ExecuteAsync(42, new Money(5m), "Test Game");

        // Alert rules should NOT be queried (implies evaluator also not called)
        alertRuleRepo.Verify(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>()), Times.Never);
        // Publisher should NOT be called
        notificationPublisher.Verify(
            n => n.PublishAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()),
            Times.Never);
        // Price save should still happen
        gameRepo.Verify(r => r.SaveAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_alertsEnabledByDefault_skipsNothing()
    {
        var game = new Game(new SteamAppId(42));
        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync(game);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>())).ReturnsAsync(new[] { rule });

        var notificationPublisher = new Mock<INotificationPublisher>();
        var evaluator = new PriceAlertEvaluator();

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["RabbitMQ:AlertsEnabled"]).Returns("true");

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo.Object,
            alertRuleRepo.Object,
            notificationPublisher.Object,
            evaluator,
            config.Object);

        // Price is below threshold
        await useCase.ExecuteAsync(42, new Money(5m), "Test Game");

        // Alert rules should be queried and publisher called
        alertRuleRepo.Verify(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>()), Times.Once);
        notificationPublisher.Verify(
            n => n.PublishAsync(rule.AlertRuleId, "user-1", 42, 5m, "EUR"),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_creates_new_game_when_not_found()
    {
        var gameRepo = new Mock<IGameRepository>();
        gameRepo.Setup(r => r.GetAsync(It.IsAny<SteamAppId>())).ReturnsAsync((Game?)null);

        var alertRuleRepo = new Mock<IAlertRuleRepository>();
        alertRuleRepo.Setup(r => r.GetActiveRulesForAsync(It.IsAny<SteamAppId>())).ReturnsAsync(Array.Empty<AlertRule>());
        var notificationPublisher = new Mock<INotificationPublisher>();
        var evaluator = new PriceAlertEvaluator();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["RabbitMQ:AlertsEnabled"]).Returns("true");

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo.Object,
            alertRuleRepo.Object,
            notificationPublisher.Object,
            evaluator,
            config.Object);

        await useCase.ExecuteAsync(42, new Money(9.99m), "New Game");

        gameRepo.Verify(r => r.SaveAsync(It.Is<Game>(g =>
            g.AppId.Value == 42 &&
            g.Name == "New Game")), Times.Once);
    }
}
