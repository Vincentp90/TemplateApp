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

    public ProcessPriceCheckUseCase(
        IGameRepository gameRepo,
        IAlertRuleRepository alertRuleRepo,
        INotificationPublisher notificationPublisher)
        : this(gameRepo, alertRuleRepo, notificationPublisher, new PriceAlertEvaluator()) { }

    public ProcessPriceCheckUseCase(
        IGameRepository gameRepo,
        IAlertRuleRepository alertRuleRepo,
        INotificationPublisher notificationPublisher,
        PriceAlertEvaluator evaluator)
    {
        _gameRepo = gameRepo;
        _alertRuleRepo = alertRuleRepo;
        _notificationPublisher = notificationPublisher;
        _evaluator = evaluator;
    }

    public async Task ExecuteAsync(int appId, Money price, string name, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepo.GetAsync(appId, cancellationToken) ?? new Game(appId);

        game.ApplyPriceUpdate(price, name, DateTimeOffset.UtcNow);
        await _gameRepo.SaveAsync(game, cancellationToken);

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
                price.Amount,
                price.Currency);
        }
    }
}
