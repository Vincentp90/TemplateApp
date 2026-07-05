using Application.Contracts;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tests.IntegrationTests;

namespace Tests.IntegrationTests;

/// <summary>
/// Integration tests for SharedDbPriceReader using a real Postgres container
/// with SteamTracker's PascalCase tables.
/// </summary>
public class SharedDbPriceReaderIntegrationTests : IAsyncLifetime
{
    private readonly SharedDbApiFactory _factory = new();
    private ISharedDbPriceReader _priceReader = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.SeedSteamTrackerAsync();
        _priceReader = _factory.Services.GetRequiredService<ISharedDbPriceReader>();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    #region GetPricesAsync

    [Fact]
    public async Task GetPricesAsync_returnsPricesForExistingGames()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync([42, 100]);

        // Assert
        prices.Should().HaveCount(2);
        prices[42].Should().Be(new GamePrice(19.99m, "EUR", new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero)));
        prices[100].Should().Be(new GamePrice(29.99m, "EUR", new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task GetPricesAsync_returnsEmptyForNonExistentGames()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync([9999]);

        // Assert
        prices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPricesAsync_mixedExistingAndNonExisting()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync([42, 9999, 100]);

        // Assert
        prices.Should().HaveCount(2);
        prices.ContainsKey(42).Should().BeTrue();
        prices.ContainsKey(100).Should().BeTrue();
        prices.ContainsKey(9999).Should().BeFalse();
    }

    [Fact]
    public async Task GetPricesAsync_emptyInput_returnsEmpty()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync([]);

        // Assert
        prices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPricesAsync_nullInput_returnsEmpty()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync((IEnumerable<int>)null!);

        // Assert
        prices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPricesAsync_returnsNullPriceForF2PGame()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync([200]);

        // Assert
        prices.Should().HaveCount(1);
        prices[200].Amount.Should().BeNull();
        prices[200].Currency.Should().BeNull();
        prices[200].LastCheckedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetPricesAsync_excludesInactiveTrackedGames()
    {
        // App 300 is tracked but not active — it has no price row, so it should be excluded
        var prices = await _priceReader.GetPricesAsync([300]);

        prices.Should().BeEmpty();
    }

    #endregion

    #region GetAlertRulesAsync

    [Fact]
    public async Task GetAlertRulesAsync_returnsActiveRulesForUser()
    {
        // Act
        var rules = await _priceReader.GetAlertRulesAsync("user-1");

        // Assert
        rules.Should().HaveCount(2); // user-1 has 2 active rules (for apps 42 and 100)
        rules[42].Should().Be(new AlertRuleInfo(
            new Guid("a0000000-0000-0000-0000-000000000001"),
            15.00m, "EUR"));
        rules[100].Should().Be(new AlertRuleInfo(
            new Guid("a0000000-0000-0000-0000-000000000002"),
            25.00m, "EUR"));
    }

    [Fact]
    public async Task GetAlertRulesAsync_excludesInactiveRules()
    {
        // user-1 has an inactive rule for app 200
        var rules = await _priceReader.GetAlertRulesAsync("user-1");

        rules.ContainsKey(200).Should().BeFalse();
    }

    [Fact]
    public async Task GetAlertRulesAsync_returnsRulesForDifferentUser()
    {
        // user-2 has one active rule for app 42
        var rules = await _priceReader.GetAlertRulesAsync("user-2");

        rules.Should().HaveCount(1);
        rules[42].Should().Be(new AlertRuleInfo(
            new Guid("a0000000-0000-0000-0000-000000000004"),
            10.00m, "EUR"));
    }

    [Fact]
    public async Task GetAlertRulesAsync_returnsEmptyForUnknownUser()
    {
        // Act
        var rules = await _priceReader.GetAlertRulesAsync("nonexistent-user");

        // Assert
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlertRulesAsync_emptyInput_returnsEmpty()
    {
        // Act
        var rules = await _priceReader.GetAlertRulesAsync("");

        // Assert
        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlertRulesAsync_nullInput_returnsEmpty()
    {
        // Act
        var rules = await _priceReader.GetAlertRulesAsync(null!);

        // Assert
        rules.Should().BeEmpty();
    }

    #endregion
}
