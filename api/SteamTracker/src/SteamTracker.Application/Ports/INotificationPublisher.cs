using System.Threading;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — dispatches alert notifications.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(Guid alertRuleId, string userId, int appId, decimal price, string currency, CancellationToken cancellationToken = default);
}
