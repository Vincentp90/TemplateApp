using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.Services;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Tests;

public class PriceAlertEvaluatorTests
{
    [Fact]
    public void Evaluate_returns_rules_that_trigger()
    {
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(5m), "Game", DateTimeOffset.UtcNow);

        var rules = new[]
        {
            new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m)),
            new AlertRule(Guid.NewGuid(), "user-2", new SteamAppId(42), new Money(3m)),
        };

        var evaluator = new PriceAlertEvaluator();
        var triggered = evaluator.Evaluate(game, rules);

        triggered.Should().ContainSingle()
            .Which.UserId.Should().Be("user-1");
    }

    [Fact]
    public void Evaluate_returns_empty_when_no_rules_trigger()
    {
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(20m), "Game", DateTimeOffset.UtcNow);

        var rules = new[]
        {
            new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m)),
            new AlertRule(Guid.NewGuid(), "user-2", new SteamAppId(42), new Money(5m)),
        };

        var evaluator = new PriceAlertEvaluator();
        var triggered = evaluator.Evaluate(game, rules);

        triggered.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_returns_all_matching_rules()
    {
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(5m), "Game", DateTimeOffset.UtcNow);

        var rules = new[]
        {
            new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m)),
            new AlertRule(Guid.NewGuid(), "user-2", new SteamAppId(42), new Money(7m)),
            new AlertRule(Guid.NewGuid(), "user-3", new SteamAppId(42), new Money(3m)),
        };

        var evaluator = new PriceAlertEvaluator();
        var triggered = evaluator.Evaluate(game, rules);

        triggered.Should().HaveCount(2);
    }

    [Fact]
    public void Evaluate_filters_inactive_rules()
    {
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(5m), "Game", DateTimeOffset.UtcNow);

        var rules = new[]
        {
            new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m)),
        };
        rules[0].Deactivate();

        var evaluator = new PriceAlertEvaluator();
        var triggered = evaluator.Evaluate(game, rules);

        triggered.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_handles_free_price()
    {
        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(Money.Free, "Free Game", DateTimeOffset.UtcNow);

        var rules = new[]
        {
            new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m)),
        };

        var evaluator = new PriceAlertEvaluator();
        var triggered = evaluator.Evaluate(game, rules);

        triggered.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_handles_null_price()
    {
        var game = new Game(new SteamAppId(42));
        // price is null (never fetched)

        var rules = new[]
        {
            new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m)),
        };

        var evaluator = new PriceAlertEvaluator();
        var triggered = evaluator.Evaluate(game, rules);

        triggered.Should().BeEmpty();
    }
}
