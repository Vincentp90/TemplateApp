using FluentAssertions;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Services;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;

namespace SteamTracker.Infrastructure.Tests;

public class UseCasesIntegrationTests : IDisposable
{
    private readonly SteamTrackerDbContext _context;

    public UseCasesIntegrationTests()
    {
        _context = TestDbContextFactory.Create();
    }

    public void Dispose()
    {
        TestDbContextFactory.Dispose(_context);
    }

    [Fact]
    public async Task ProcessPriceCheckUseCase_full_flow()
    {
        // Arrange — seed an existing game and alert rule
        var appId = new SteamAppId(42);
        var game = new Game(appId);
        game.ApplyPriceUpdate(new Money(29.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);
        _context.Games.Add(game);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", appId, new Money(20m));
        _context.AlertRules.Add(rule);

        await _context.SaveChangesAsync();

        // Create repositories that use the same context
        var gameRepo = new Infrastructure.Repositories.GameRepository(_context);
        var alertRuleRepo = new Infrastructure.Repositories.AlertRuleRepository(_context);
        var notificationPublisher = new MockNotificationPublisher();
        var evaluator = new PriceAlertEvaluator();

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo,
            alertRuleRepo,
            notificationPublisher,
            evaluator);

        // Act — price drops below threshold
        await useCase.ExecuteAsync(42, new Money(15m, "EUR"), "Test Game", CancellationToken.None);

        // Assert — price updated
        var updatedGame = await _context.Games.FindAsync(appId);
        updatedGame!.CurrentPrice.Should().NotBeNull();
        updatedGame.CurrentPrice!.Value.Amount.Should().Be(15m);
        updatedGame.LastCheckedAt.Should().NotBeNull();

        // Assert — notification was sent
        notificationPublisher.Messages.Should().ContainSingle();
        notificationPublisher.Messages[0].UserId.Should().Be("user-1");
        notificationPublisher.Messages[0].Price.Should().Be(15m);
        notificationPublisher.Messages[0].AppId.Should().Be(42);
    }

    [Fact]
    public async Task ProcessPriceCheckUseCase_no_alert_when_price_above_threshold()
    {
        // Arrange
        var appId = new SteamAppId(42);
        var game = new Game(appId);
        game.ApplyPriceUpdate(new Money(29.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);
        _context.Games.Add(game);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", appId, new Money(40m));
        _context.AlertRules.Add(rule);

        await _context.SaveChangesAsync();

        var gameRepo = new Infrastructure.Repositories.GameRepository(_context);
        var alertRuleRepo = new Infrastructure.Repositories.AlertRuleRepository(_context);
        var notificationPublisher = new MockNotificationPublisher();
        var evaluator = new PriceAlertEvaluator();

        var useCase = new ProcessPriceCheckUseCase(
            gameRepo,
            alertRuleRepo,
            notificationPublisher,
            evaluator);

        // Act — price is above threshold (no alert)
        await useCase.ExecuteAsync(42, new Money(50m, "EUR"), "Test Game", CancellationToken.None);

        // Assert — no notifications
        notificationPublisher.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleWishlistItemAddedUseCase_creates_tracked_game()
    {
        // Arrange
        var trackedGameRepo = new Infrastructure.Repositories.TrackedGameRepository(_context);
        var publisher = new MockPriceCheckJobPublisher();

        var useCase = new HandleWishlistItemAddedUseCase(
            trackedGameRepo,
            publisher);

        // Act
        await useCase.ExecuteAsync("user-1", 42, DateTimeOffset.UtcNow, CancellationToken.None);

        // Assert
        var saved = await trackedGameRepo.GetAsync(new SteamAppId(42));
        saved.Should().NotBeNull();
        saved!.IsActive.Should().BeTrue();

        publisher.Enqueued.Should().Contain(42);
    }

    [Fact]
    public async Task HandleWishlistItemRemovedUseCase_deactivates_game_and_rules()
    {
        // Arrange
        var appId = new SteamAppId(42);
        var trackedGame = TrackedGame.StartTracking(appId, DateTimeOffset.UtcNow);
        _context.TrackedGames.Add(trackedGame);

        var rule = new AlertRule(Guid.NewGuid(), "user-1", appId, new Money(10m));
        _context.AlertRules.Add(rule);

        await _context.SaveChangesAsync();

        var trackedGameRepo = new Infrastructure.Repositories.TrackedGameRepository(_context);
        var alertRuleRepo = new Infrastructure.Repositories.AlertRuleRepository(_context);

        var useCase = new HandleWishlistItemRemovedUseCase(
            trackedGameRepo,
            alertRuleRepo);

        // Act
        await useCase.ExecuteAsync("user-1", 42, CancellationToken.None);

        // Assert
        var updatedGame = await trackedGameRepo.GetAsync(appId);
        updatedGame!.IsActive.Should().BeFalse();

        var updatedRule = await alertRuleRepo.GetAsync(rule.AlertRuleId);
        updatedRule!.IsActive.Should().BeFalse();
    }
}

// Test doubles for infrastructure ports
public class MockNotificationPublisher : INotificationPublisher
{
    public List<(Guid AlertRuleId, string UserId, int AppId, decimal Price, string Currency)> Messages { get; } = new();

    public Task PublishAsync(Guid alertRuleId, string userId, int appId, decimal price, string currency, CancellationToken cancellationToken = default)
    {
        Messages.Add((alertRuleId, userId, appId, price, currency));
        return Task.CompletedTask;
    }
}

public class MockPriceCheckJobPublisher : IPriceCheckJobPublisher
{
    public List<int> Enqueued { get; } = new();

    public Task EnqueueAsync(int appId, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(appId);
        return Task.CompletedTask;
    }
}
