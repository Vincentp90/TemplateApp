using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class PriceSnapshotTests
{
    [Fact]
    public void Constructor_sets_all_properties()
    {
        var price = new Money(9.99m, "EUR");
        var at = DateTimeOffset.UtcNow;
        var snapshot = new PriceSnapshot(1, price, 25, at);

        snapshot.Price.Should().Be(price);
        snapshot.DiscountPercent.Should().Be(25);
        snapshot.CapturedAt.Should().Be(at);
    }

    [Fact]
    public void Constructor_defaults_discount_to_zero()
    {
        var price = new Money(9.99m);
        var at = DateTimeOffset.UtcNow;
        var snapshot = new PriceSnapshot(1, price, at);

        snapshot.DiscountPercent.Should().Be(0);
    }

    [Fact]
    public void Snapshot_has_unique_id()
    {
        var snapshot1 = new PriceSnapshot(1, new Money(1m), DateTimeOffset.UtcNow);
        var snapshot2 = new PriceSnapshot(1, new Money(1m), DateTimeOffset.UtcNow);

        snapshot1.SnapshotId.Should().NotBe(snapshot2.SnapshotId);
    }
}
