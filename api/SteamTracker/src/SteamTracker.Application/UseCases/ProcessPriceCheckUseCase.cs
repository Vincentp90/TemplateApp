using Microsoft.Extensions.Configuration;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Services;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Called by PriceCheckWorker after fetching price from Steam.
/// Saves the price, evaluates alert rules, and dispatches notifications.
/// </summary>
public class ProcessPriceCheckUseCase : IProcessPriceCheckUseCase
{
    private readonly IGameRepository _gameRepo;
    private readonly IAlertRuleRepository _alertRuleRepo;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly PriceAlertEvaluator _evaluator;
    private readonly bool _alertsEnabled;

    public ProcessPriceCheckUseCase(
        IGameRepository gameRepo,
        IAlertRuleRepository alertRuleRepo,
        INotificationPublisher notificationPublisher,
        PriceAlertEvaluator evaluator)
        : this(gameRepo, alertRuleRepo, notificationPublisher, evaluator, null) { }

    public ProcessPriceCheckUseCase(
        IGameRepository gameRepo,
        IAlertRuleRepository alertRuleRepo,
        INotificationPublisher notificationPublisher,
        PriceAlertEvaluator evaluator,
        IConfiguration? configuration)
    {
        _gameRepo = gameRepo;
        _alertRuleRepo = alertRuleRepo;
        _notificationPublisher = notificationPublisher;
        _evaluator = evaluator;
        _alertsEnabled = configuration?["RabbitMQ:AlertsEnabled"] != "false";
    }

    public async Task ExecuteAsync(int appId, Money? price, string name, bool isUnavailable, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepo.GetAsync(appId, cancellationToken) ?? new Game(appId);

        if (isUnavailable)
        {
            game.MarkUnavailable();
        }
        else if (price != null)
        {
            game.ApplyPriceUpdate(price.Value, name, DateTimeOffset.UtcNow);
        }
        else
        {
            // Price data available but no price_overview (free game) — still update name and timestamp
            game.ApplyNameUpdate(name, DateTimeOffset.UtcNow);
        }

        await _gameRepo.SaveAsync(game, cancellationToken);

        if (isUnavailable || !_alertsEnabled)
            return;

        if (price == null) return;

        var rules = await _alertRuleRepo.GetActiveRulesForAsync(game.AppId, cancellationToken);
        var triggered = _evaluator.Evaluate(game, rules);

        foreach (var rule in triggered)
        {
            rule.MarkTriggered(DateTimeOffset.UtcNow);
            await _alertRuleRepo.SaveAsync(rule, cancellationToken);

            await _notificationPublisher.PublishAsync(
                rule.AlertRuleId,
                rule.UserId,
                game.AppId.Value,
                price.Value.Amount,
                price.Value.Currency);
        }
    }
}
