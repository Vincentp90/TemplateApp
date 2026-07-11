using System.Threading;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Deletes an existing alert rule.
/// </summary>
public interface IDeleteAlertRuleUseCase
{
    Task ExecuteAsync(string userId, Guid alertRuleId, CancellationToken cancellationToken = default);
}
