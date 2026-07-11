using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Entities;

/// <summary>
/// Append-only snapshot of a game's price at a point in time.
/// Child entity of Game.
/// </summary>
public class PriceSnapshot
{
    public Guid SnapshotId { get; private set; } = Guid.NewGuid();
    public SteamAppId GameId { get; private set; }
    public Money Price { get; private set; }
    public int DiscountPercent { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }

    private PriceSnapshot() { } // EF Core constructor

    public PriceSnapshot(SteamAppId gameId, Money price, int discountPercent, DateTimeOffset capturedAt)
    {
        GameId = gameId;
        Price = price;
        DiscountPercent = discountPercent;
        CapturedAt = capturedAt;
    }

    public PriceSnapshot(SteamAppId gameId, Money price, DateTimeOffset capturedAt)
        : this(gameId, price, 0, capturedAt) { }
}
