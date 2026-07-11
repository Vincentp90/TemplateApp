using System.Threading;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — persistence for AlertRule aggregate.
/// </summary>
public interface IAlertRuleRepository
{
    Task<IReadOnlyList<AlertRule>> GetActiveRulesForAsync(SteamAppId appId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertRule>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<AlertRule?> GetAsync(Guid alertRuleId, CancellationToken cancellationToken = default);
    Task SaveAsync(AlertRule alertRule, CancellationToken cancellationToken = default);
    Task DeleteAsync(AlertRule alertRule, CancellationToken cancellationToken = default);
}
