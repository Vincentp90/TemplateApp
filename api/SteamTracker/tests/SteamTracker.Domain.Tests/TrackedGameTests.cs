using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class TrackedGameTests
{
    [Fact]
    public void StartTracking_creates_active_game()
    {
        var at = DateTimeOffset.UtcNow;
        var game = TrackedGame.StartTracking(new SteamAppId(12345), at);

        game.AppId.Should().Be(new SteamAppId(12345));
        game.IsActive.Should().BeTrue();
        game.TrackedSince.Should().Be(at);
    }

    [Fact]
    public void StopTracking_deactivates_game()
    {
        var game = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        game.StopTracking();

        game.IsActive.Should().BeFalse();
    }

    [Fact]
    public void StopTracking_on_inactive_is_no_op()
    {
        var game = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        game.StopTracking();
        game.StopTracking(); // second call

        game.IsActive.Should().BeFalse();
    }
}
