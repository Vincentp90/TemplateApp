using System.Threading;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — external Steam Store HTTP client.
/// </summary>
public interface ISteamStoreClient
{
    /// <summary>
    /// Returns (price, name, isUnavailable) for the given app ID.
    /// When price is null and isUnavailable is true, the game no longer exists on Steam.
    /// Throws SteamRateLimitException on HTTP 429.
    /// </summary>
    Task<SteamPriceResult?> FetchPriceAsync(int appId, CancellationToken cancellationToken = default);
}
