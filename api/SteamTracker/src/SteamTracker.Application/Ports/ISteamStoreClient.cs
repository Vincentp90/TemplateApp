using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Driven port — external Steam Store HTTP client.
/// </summary>
public interface ISteamStoreClient
{
    /// <summary>
    /// Returns (price, name) for the given app ID.
    /// Returns null if the app is not found.
    /// Throws SteamRateLimitException on HTTP 429.
    /// </summary>
    Task<(Money Price, string Name)?> FetchPriceAsync(int appId);
}
