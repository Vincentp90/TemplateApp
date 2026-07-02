using System.Threading;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Creates an alert rule for a user on a tracked game.
/// Validates that the TrackedGame exists and is active.
/// </summary>
public interface ISetAlertRuleUseCase
{
    Task ExecuteAsync(string userId, int appId, decimal thresholdAmount, string currency = "EUR", CancellationToken cancellationToken = default);
}
