using FluentAssertions;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;

namespace SteamTracker.Infrastructure.Tests.Repositories;

public class AlertRuleRepositoryTests : IDisposable
{
    private readonly SteamTrackerDbContext _context;
    private readonly AlertRuleRepository _repository;

    public AlertRuleRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new AlertRuleRepository(_context);
    }

    public void Dispose()
    {
        TestDbContextFactory.Dispose(_context);
    }

    [Fact]
    public async Task GetActiveRulesForAsync_returns_active_rules()
    {
        // Arrange
        var appId = new SteamAppId(42);
        var activeRule = new AlertRule(Guid.NewGuid(), "user-1", appId, new Money(10m));
        var inactiveRule = new AlertRule(Guid.NewGuid(), "user-1", appId, new Money(5m));
        inactiveRule.Deactivate();

        _context.AlertRules.Add(activeRule);
        _context.AlertRules.Add(inactiveRule);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveRulesForAsync(appId);

        // Assert
        results.Should().ContainSingle();
        results[0].AlertRuleId.Should().Be(activeRule.AlertRuleId);
    }

    [Fact]
    public async Task GetForUserAsync_returns_all_rules_for_user()
    {
        // Arrange
        var appId1 = new SteamAppId(42);
        var appId2 = new SteamAppId(43);
        var rule1 = new AlertRule(Guid.NewGuid(), "user-1", appId1, new Money(10m));
        var rule2 = new AlertRule(Guid.NewGuid(), "user-1", appId2, new Money(5m));
        var rule3 = new AlertRule(Guid.NewGuid(), "user-2", appId1, new Money(15m));

        _context.AlertRules.Add(rule1);
        _context.AlertRules.Add(rule2);
        _context.AlertRules.Add(rule3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetForUserAsync("user-1");

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.AlertRuleId == rule1.AlertRuleId);
        results.Should().Contain(r => r.AlertRuleId == rule2.AlertRuleId);
    }

    [Fact]
    public async Task GetAsync_returns_rule_when_exists()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = new AlertRule(ruleId, "user-1", new SteamAppId(42), new Money(10m));
        _context.AlertRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(ruleId);

        // Assert
        result.Should().NotBeNull();
        result!.AlertRuleId.Should().Be(ruleId);
        result.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetAsync_returns_null_when_not_exists()
    {
        // Act
        var result = await _repository.GetAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_inserts_new_rule()
    {
        // Act
        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        await _repository.SaveAsync(rule);

        // Assert
        var saved = await _context.AlertRules.FindAsync(rule.AlertRuleId);
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task SaveAsync_updates_existing_rule()
    {
        // Arrange
        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        _context.AlertRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        rule.Deactivate();
        await _repository.SaveAsync(rule);

        // Assert
        var updated = await _context.AlertRules.FindAsync(rule.AlertRuleId);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_removes_rule()
    {
        // Arrange
        var rule = new AlertRule(Guid.NewGuid(), "user-1", new SteamAppId(42), new Money(10m));
        _context.AlertRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(rule);

        // Assert
        var deleted = await _context.AlertRules.FindAsync(rule.AlertRuleId);
        deleted.Should().BeNull();
    }
}
