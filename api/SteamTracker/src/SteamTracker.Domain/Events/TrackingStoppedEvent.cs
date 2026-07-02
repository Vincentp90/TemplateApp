using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Events;

/// <summary>
/// Raised when a game stops being tracked.
/// </summary>
public record TrackingStoppedEvent(SteamAppId AppId);
