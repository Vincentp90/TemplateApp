using System.Threading;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — publishes price-check jobs to the message broker.
/// </summary>
public interface IPriceCheckJobPublisher
{
    Task EnqueueAsync(int appId, CancellationToken cancellationToken = default);
}
