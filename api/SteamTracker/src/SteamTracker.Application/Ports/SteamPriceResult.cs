using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Outcome of a call to the Steam store API.
///
/// A <c>null</c> <see cref="SteamPriceResult"/> returned from <c>FetchPriceAsync</c> means
/// a transient failure (network error, malformed payload, non-2xx status) — safe to retry.
///
/// A non-null result with <see cref="IsUnavailable"/> set to <c>true</c> means Steam
/// explicitly confirmed the app no longer exists (<c>success: false</c>) — this is a
/// permanent outcome and should not be retried.
/// </summary>
public sealed record SteamPriceResult
{
    public Money? Price { get; private init; }
    public string Name { get; private init; } = string.Empty;
    public bool IsUnavailable { get; private init; }

    public static SteamPriceResult WithPrice(Money price, string name) =>
        new() { Price = price, Name = name };

    public static SteamPriceResult Free(string name) =>
        new() { Price = Money.Free, Name = name };

    public static SteamPriceResult Unavailable() =>
        new() { IsUnavailable = true };
}