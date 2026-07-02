using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;

namespace SteamTracker.Infrastructure.Tests.Repositories;

public class GameRepositoryTests : IDisposable
{
    private readonly SteamTrackerDbContext _context;
    private readonly GameRepository _repository;

    public GameRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new GameRepository(_context);
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
        var game = new Game(appId);
        game.ApplyPriceUpdate(new Money(9.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(appId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AppId.Should().Be(appId);
        result.Name.Should().Be("Test Game");
        result.CurrentPrice.Should().NotBeNull();
        result.CurrentPrice!.Value.Amount.Should().Be(9.99m);
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
    public async Task SaveAsync_inserts_new_game()
    {
        // Act
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(19.99m, "USD"), "New Game", DateTimeOffset.UtcNow);
        await _repository.SaveAsync(game, CancellationToken.None);

        // Assert
        var saved = await _context.Games.FindAsync(new object[] { new SteamAppId(42) }, CancellationToken.None);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Game");
    }

    [Fact]
    public async Task SaveAsync_updates_existing_game()
    {
        // Arrange
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(9.99m, "EUR"), "Old Name", DateTimeOffset.UtcNow);
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        game.ApplyPriceUpdate(new Money(14.99m, "USD"), "Updated Name", DateTimeOffset.UtcNow);
        await _repository.SaveAsync(game, CancellationToken.None);

        // Assert
        var updated = await _context.Games.FindAsync(new object[] { new SteamAppId(42) }, CancellationToken.None);
        updated!.Name.Should().Be("Updated Name");
        updated.CurrentPrice!.Value.Amount.Should().Be(14.99m);
    }

    [Fact]
    public async Task GetAsync_includes_price_snapshots()
    {
        // Arrange
        var appId = new SteamAppId(42);
        var game = new Game(appId);
        game.ApplyPriceUpdate(new Money(9.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(appId, CancellationToken.None);

        // Assert
        result!.PriceSnapshots.Should().ContainSingle();
        result.PriceSnapshots[0].Price.Amount.Should().Be(9.99m);
        result.PriceSnapshots[0].Price.Currency.Should().Be("EUR");
    }
}
