using System.Threading;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — persistence for TrackedGame aggregate.
/// </summary>
public interface ITrackedGameRepository
{
    Task<TrackedGame?> GetAsync(SteamAppId appId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackedGame>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TrackedGame trackedGame, CancellationToken cancellationToken = default);
}
