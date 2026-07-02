using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;
using SteamTracker.Infrastructure.Tests.TestContainers;
using Testcontainers.PostgreSql;

namespace SteamTracker.Infrastructure.Tests.Repositories;

/// <summary>
/// Integration tests using real Postgres via testcontainers.
/// These tests verify EF Core mappings against a real database.
/// </summary>
public class PostgresRepositoryIntegrationTests : IAsyncLifetime
{
    private SteamTrackerDbContext? _context;

    public async Task InitializeAsync()
    {
        await PostgresContainerFixture.Instance.Container.StartAsync();
        _context = PostgresContainerFixture.Instance.CreateDbContext();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await PostgresContainerFixture.Instance.Container.StopAsync();
    }

    [Fact]
    public async Task GameRepository_roundtrip_with_real_postgres()
    {
        // Arrange
        var appId = new SteamAppId(123456);
        var repository = new GameRepository(_context!);

        // Act
        var game = new Game(appId);
        game.ApplyPriceUpdate(new Money(29.99m, "USD"), "Cyberpunk 2077", DateTimeOffset.UtcNow);
        await repository.SaveAsync(game);

        // Assert
        var fetched = await repository.GetAsync(appId);
        fetched.Should().NotBeNull();
        fetched!.AppId.Should().Be(appId);
        fetched.Name.Should().Be("Cyberpunk 2077");
        fetched.CurrentPrice.Should().NotBeNull();
        fetched.CurrentPrice!.Value.Amount.Should().Be(29.99m);
        fetched.CurrentPrice.Value.Currency.Should().Be("USD");
        fetched.PriceSnapshots.Should().ContainSingle();
        fetched.PriceSnapshots[0].Price.Amount.Should().Be(29.99m);
    }

    [Fact]
    public async Task TrackedGameRepository_roundtrip_with_real_postgres()
    {
        // Arrange
        var appId = new SteamAppId(789);
        var repository = new TrackedGameRepository(_context!);

        // Act
        var trackedGame = TrackedGame.StartTracking(appId, DateTimeOffset.UtcNow);
        await repository.SaveAsync(trackedGame);

        // Assert
        var fetched = await repository.GetAsync(appId);
        fetched.Should().NotBeNull();
        fetched!.IsActive.Should().BeTrue();

        // Deactivate
        trackedGame.StopTracking();
        await repository.SaveAsync(trackedGame);

        var deactivated = await repository.GetAsync(appId);
        deactivated!.IsActive.Should().BeFalse();

        var activeGames = await repository.GetActiveAsync();
        activeGames.Should().BeEmpty();
    }

    [Fact]
    public async Task AlertRuleRepository_roundtrip_with_real_postgres()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var appId = new SteamAppId(42);
        var repository = new AlertRuleRepository(_context!);

        // Act
        var rule = new AlertRule(ruleId, "user-1", appId, new Money(15m, "EUR"));
        await repository.SaveAsync(rule);

        // Assert
        var fetched = await repository.GetAsync(ruleId);
        fetched.Should().NotBeNull();
        fetched!.UserId.Should().Be("user-1");
        fetched.TriggerBelowPrice.Amount.Should().Be(15m);
        fetched.TriggerBelowPrice.Currency.Should().Be("EUR");
        fetched.IsActive.Should().BeTrue();

        // Deactivate and verify
        rule.Deactivate();
        await repository.SaveAsync(rule);

        var deactivated = await repository.GetAsync(ruleId);
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Multiple_price_snapshots_are_persisted()
    {
        // Arrange
        var appId = new SteamAppId(999);
        var repository = new GameRepository(_context!);

        // Act
        var game = new Game(appId);
        game.ApplyPriceUpdate(new Money(49.99m, "USD"), "Game", DateTimeOffset.UtcNow);
        await repository.SaveAsync(game);

        // Simulate price change
        game.ApplyPriceUpdate(new Money(29.99m, "USD"), "Game", DateTimeOffset.UtcNow.AddHours(1));
        await repository.SaveAsync(game);

        // Assert
        var fetched = await repository.GetAsync(appId);
        fetched!.PriceSnapshots.Should().HaveCount(2);
        fetched.PriceSnapshots[0].Price.Amount.Should().Be(49.99m);
        fetched.PriceSnapshots[1].Price.Amount.Should().Be(29.99m);
    }
}
