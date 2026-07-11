using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;

namespace SteamTracker.Infrastructure.Tests.Repositories;

public class TrackedGameRepositoryTests : IDisposable
{
    private readonly SteamTrackerDbContext _context;
    private readonly TrackedGameRepository _repository;

    public TrackedGameRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new TrackedGameRepository(_context);
    }

    public void Dispose()
    {
        TestDbContextFactory.Dispose(_context);
    }

    [Fact]
    public async Task GetAsync_returns_game_when_exists()
    {
        // Arrange
        var appId = new SteamAppId(42);
        var trackedGame = TrackedGame.StartTracking(appId, DateTimeOffset.UtcNow);
        _context.TrackedGames.Add(trackedGame);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(appId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AppId.Should().Be(appId);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_returns_null_when_not_exists()
    {
        // Act
        var result = await _repository.GetAsync(new SteamAppId(999), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAsync_returns_only_active_games()
    {
        // Arrange
        var activeGame = TrackedGame.StartTracking(new SteamAppId(1), DateTimeOffset.UtcNow);
        var inactiveGame = TrackedGame.StartTracking(new SteamAppId(2), DateTimeOffset.UtcNow);
        inactiveGame.StopTracking();

        _context.TrackedGames.Add(activeGame);
        _context.TrackedGames.Add(inactiveGame);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveAsync(CancellationToken.None);

        // Assert
        results.Should().ContainSingle();
        results[0].AppId.Should().Be(new SteamAppId(1));
    }

    [Fact]
    public async Task SaveAsync_inserts_new_record()
    {
        // Act
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        await _repository.SaveAsync(trackedGame, CancellationToken.None);

        // Assert
        var saved = await _context.TrackedGames.FindAsync(new object[] { new SteamAppId(42) }, CancellationToken.None);
        saved.Should().NotBeNull();
        saved!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_updates_existing_record()
    {
        // Arrange
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        _context.TrackedGames.Add(trackedGame);
        await _context.SaveChangesAsync();

        // Act
        trackedGame.StopTracking();
        await _repository.SaveAsync(trackedGame, CancellationToken.None);

        // Assert
        var updated = await _context.TrackedGames.FindAsync(new object[] { new SteamAppId(42) }, CancellationToken.None);
        updated!.IsActive.Should().BeFalse();
    }
}
