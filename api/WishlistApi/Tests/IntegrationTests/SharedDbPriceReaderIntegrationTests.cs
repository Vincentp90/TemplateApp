using Application.Contracts;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tests.IntegrationTests;

namespace Tests.IntegrationTests;

/// <summary>
/// Integration tests for SharedDbPriceReader using a real Postgres container
/// with SteamTracker's snake_case tables (matching EF Core naming convention).
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
    public async Task GetPricesAsync_readsLastCheckedAt_fromTimestamptz_column()
    {
        // Arrange — insert a game row using Npgsql directly with DateTimeOffset
        // (mimics what EF Core writes via SteamTracker.Worker)
        var connStr = SharedDbFixture.Instance.SteamTrackerConnectionString;
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        const string insertSql = @"
            INSERT INTO ""games"" (""app_id"", ""name"", ""current_price"", ""last_checked_at"")
            VALUES (@appId, @name, @priceStr, @checkedAt)
            ON CONFLICT (""app_id"") DO NOTHING";
        await conn.ExecuteAsync(insertSql, new
        {
            appId = 7777,
            name = "Timestamptz Test Game",
            priceStr = "49.99|EUR",
            checkedAt = new DateTimeOffset(2026, 7, 7, 10, 30, 0, TimeSpan.Zero).UtcDateTime
        });

        // Act
        var prices = await _priceReader.GetPricesAsync([7777]);

        // Assert — LastCheckedAt should NOT be null when the column has a timestamptz value
        prices.Should().HaveCount(1);
        prices[7777].Amount.Should().Be(49.99m);
        prices[7777].LastCheckedAt.Should().NotBeNull(
            "Dapper should map PostgreSQL timestamptz to DateTimeOffset, not DateTime");
        prices[7777].LastCheckedAt.Should().BeCloseTo(
            new DateTimeOffset(2026, 7, 7, 10, 30, 0, TimeSpan.Zero), TimeSpan.FromSeconds(1));
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
    public async Task GetPricesAsync_returnsZeroPriceForFreeGame()
    {
        // Act
        var prices = await _priceReader.GetPricesAsync([200]);

        // Assert
        prices.Should().HaveCount(1);
        prices[200].Amount.Should().Be(0m);
        prices[200].Currency.Should().Be("EUR");
        prices[200].LastCheckedAt.Should().NotBeNull();
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
