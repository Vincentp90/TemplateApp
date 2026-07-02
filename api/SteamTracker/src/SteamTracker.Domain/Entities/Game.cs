using SteamTracker.Domain.Events;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Entities;

/// <summary>
/// Aggregate root — holds price data for a unique AppId.
/// </summary>
public class Game
{
    public SteamAppId AppId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money? CurrentPrice { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }

    public List<PriceSnapshot> PriceSnapshots { get; private set; } = new();

    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Game() { } // EF Core constructor

    public Game(SteamAppId appId)
    {
        AppId = appId;
    }

    public void ApplyPriceUpdate(Money newPrice, string name, DateTimeOffset at)
    {
        var oldPrice = CurrentPrice;
        Name = name;
        CurrentPrice = newPrice;
        LastCheckedAt = at;

        PriceSnapshots.Add(new PriceSnapshot(AppId.Value, newPrice, at));
        _domainEvents.Add(new PriceUpdatedEvent(AppId, oldPrice, newPrice, at));
    }
}
