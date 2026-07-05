using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Exceptions;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Creates an alert rule for a user on a tracked game.
/// Validates that TrackedGame exists and is active.
/// </summary>
public class SetAlertRuleUseCase : ISetAlertRuleUseCase
{
    private readonly ITrackedGameRepository _trackedGameRepo;
    private readonly IAlertRuleRepository _alertRuleRepo;

    public SetAlertRuleUseCase(
        ITrackedGameRepository trackedGameRepo,
        IAlertRuleRepository alertRuleRepo)
    {
        _trackedGameRepo = trackedGameRepo;
        _alertRuleRepo = alertRuleRepo;
    }

    public async Task ExecuteAsync(string userId, int appId, decimal thresholdAmount, string currency = "EUR", CancellationToken cancellationToken = default)
    {
        var trackedGame = await _trackedGameRepo.GetAsync(appId, cancellationToken);
        if (trackedGame is null || !trackedGame.IsActive)
            throw new TrackingNotFoundException($"No active tracking for AppId {appId}.");

        var rule = new AlertRule(
            Guid.NewGuid(),
            userId,
            appId,
            new Money(thresholdAmount, currency));

        await _alertRuleRepo.SaveAsync(rule, cancellationToken);
    }
}
