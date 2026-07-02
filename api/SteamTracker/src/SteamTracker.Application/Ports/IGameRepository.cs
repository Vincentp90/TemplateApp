using System.Threading;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — persistence for Game aggregate.
/// </summary>
public interface IGameRepository
{
    Task<Game?> GetAsync(SteamAppId appId, CancellationToken cancellationToken = default);
    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
