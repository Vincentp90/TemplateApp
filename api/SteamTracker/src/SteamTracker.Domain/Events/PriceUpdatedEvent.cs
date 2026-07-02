using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Events;

/// <summary>
/// Raised when a game's price is updated.
/// </summary>
public record PriceUpdatedEvent(
    SteamAppId AppId,
    Money? OldPrice,
    Money NewPrice,
    DateTimeOffset CapturedAt);
