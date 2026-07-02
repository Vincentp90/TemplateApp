using SteamTracker.Domain.Events;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Entities;

/// <summary>
/// Aggregate root — represents a game that is being tracked.
/// Created from wishlist events; its sole purpose is to indicate whether
/// we should fetch prices for this AppId.
/// </summary>
public class TrackedGame
{
    public SteamAppId AppId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset TrackedSince { get; private set; }

    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private TrackedGame() { } // EF Core constructor

    private TrackedGame(SteamAppId appId, DateTimeOffset trackedSince)
    {
        AppId = appId;
        IsActive = true;
        TrackedSince = trackedSince;
    }

    public static TrackedGame StartTracking(SteamAppId appId, DateTimeOffset trackedSince)
        => new(appId, trackedSince);

    public void StopTracking()
    {
        if (!IsActive) return;

        IsActive = false;
        _domainEvents.Add(new TrackingStoppedEvent(AppId));
    }
}
