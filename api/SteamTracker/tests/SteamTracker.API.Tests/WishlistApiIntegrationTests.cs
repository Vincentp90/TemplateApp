using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SteamTracker.API.Tests.Helpers;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;
using SteamTracker.Application.Ports;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

/// <summary>
/// Collection marker — ensures all API integration tests run sequentially.
/// </summary>
public class ApiIntegrationCollection { }

/// <summary>
/// Integration tests for SteamTracker API endpoints using WebApplicationFactory + testcontainers.
/// All tests in this class share the same DB and must run sequentially.
/// </summary>
[Collection("ApiIntegration")]
public class WishlistApiIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public WishlistApiIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client => _factory.GetOrCreateClient();

    /// <summary>
    /// Helper to query DB state within a scoped lifetime.
    /// </summary>
    private async Task<T> WithDbContextAsync<T>(Func<SteamTrackerDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SteamTrackerDbContext>();
        return await action(db);
    }

    /// <summary>
    /// Helper to execute DB operations within a scoped lifetime (void return).
    /// </summary>
    private async Task WithDbContextAsync(Func<SteamTrackerDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SteamTrackerDbContext>();
        await action(db);
    }

    [Fact]
    public async Task GET_wishlist_ReturnsPrices()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("X-Internal-UserId", "test-user");

        // Act
        var response = await Client.GetAsync("/api/wishlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        content.Should().NotBeNull();
        content.Should().HaveCount(1);
        content![0].GetProperty("appId").GetInt32().Should().Be(42);
        content[0].GetProperty("currentPrice").GetDouble().Should().Be(19.99);
    }

    [Fact]
    public async Task GET_wishlist_EmptyWhenNoTrackedGames()
    {
        // Arrange — clear tracked games
        await WithDbContextAsync(async db =>
        {
            await db.TrackedGames.ExecuteDeleteAsync();
        });

        // Act
        Client.DefaultRequestHeaders.Add("X-Internal-UserId", "unknown-user");
        var response = await Client.GetAsync("/api/wishlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        content.Should().BeEmpty();

        // Re-seed so subsequent tests have data
        using var seedScope = _factory.Services.CreateScope();
        var sp = seedScope.ServiceProvider;
        var seedDb = sp.GetRequiredService<SteamTrackerDbContext>();
        var trackedGameRepo = sp.GetRequiredService<ITrackedGameRepository>();
        var gameRepo = sp.GetRequiredService<IGameRepository>();

        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        await trackedGameRepo.SaveAsync(trackedGame);

        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(19.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);
        await gameRepo.SaveAsync(game);
    }

    [Fact]
    public async Task POST_alertRule_CreatesRule()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("X-Internal-UserId", "test-user");

        // Act
        var response = await Client.PostAsync("/api/games/42/alert?thresholdAmount=15.0&currency=EUR", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify in DB
        await WithDbContextAsync(async db =>
        {
            var rules = await db.AlertRules.ToListAsync();
            var rule = rules.FirstOrDefault(r => r.UserId == "test-user" && r.AppId.Value == 42);
            rule.Should().NotBeNull();
            rule!.TriggerBelowPrice.Amount.Should().Be(15.0m);
        });
    }

    [Fact]
    public async Task POST_alertRule_ThrowsWhenNotTracked()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("X-Internal-UserId", "test-user");

        // Act — app 99999 is not tracked
        var response = await Client.PostAsync("/api/games/99999/alert?thresholdAmount=10.0", null);

        // Assert — TrackingNotFoundException → 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_alertRule_DeletesRule()
    {
        var uniqueUserId = $"delete-test-user-{Guid.NewGuid():N}";
        var uniqueAppId = 50000;
        var ruleId = Guid.NewGuid();

        // Arrange — ensure tracked game and alert rule exist in DB
        using var seedScope = _factory.Services.CreateScope();
        var seedSp = seedScope.ServiceProvider;
        var seedDb = seedSp.GetRequiredService<SteamTrackerDbContext>();
        var trackedGameRepo = seedSp.GetRequiredService<ITrackedGameRepository>();

        var existingTg = await trackedGameRepo.GetAsync(new SteamAppId(uniqueAppId));
        if (existingTg is null)
        {
            var tg = TrackedGame.StartTracking(new SteamAppId(uniqueAppId), DateTimeOffset.UtcNow);
            await trackedGameRepo.SaveAsync(tg);
        }

        var existingRule = await seedDb.AlertRules.FindAsync(ruleId);
        if (existingRule is null)
        {
            var rule = new AlertRule(ruleId, uniqueUserId, new SteamAppId(uniqueAppId), new Money(10.0m, "EUR"));
            seedDb.AlertRules.Add(rule);
            await seedDb.SaveChangesAsync();
        }

        // Brief wait to ensure DB commit completes before API call
        await Task.Delay(100);

        // Act — delete via API (use a fresh client to avoid connection reuse issues)
        using var deleteClient = _factory.CreateClient();
        deleteClient.DefaultRequestHeaders.Add("X-Internal-UserId", uniqueUserId);
        var response = await deleteClient.DeleteAsync($"/api/alert/{ruleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await WithDbContextAsync(async d =>
        {
            var found = await d.AlertRules.FindAsync(ruleId);
            found.Should().BeNull();
        });
    }



    [Fact]
    public async Task POST_internal_priceCheck_ProcessesPrice()
    {
        // Arrange — ensure tracked game exists
        await WithDbContextAsync(async db =>
        {
            var games = await db.TrackedGames.ToListAsync();
            if (!games.Any(tg => tg.AppId.Value == 777))
            {
                var game = TrackedGame.StartTracking(new SteamAppId(777), DateTimeOffset.UtcNow);
                await db.TrackedGames.AddAsync(game);
                await db.SaveChangesAsync();
            }
        });

        // Act
        var payload = new { appId = 777, price = 5.50m, name = "Cheap Game" };
        var response = await Client.PostAsJsonAsync("/api/internal/price-check", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify price was saved
        await WithDbContextAsync(async db =>
        {
            var savedGame = (await db.Games.ToListAsync()).FirstOrDefault(g => g.AppId.Value == 777);
            savedGame.Should().NotBeNull();
            savedGame!.CurrentPrice!.Value.Amount.Should().Be(5.50m);
        });
    }

    [Fact]
    public async Task POST_internal_wishlistAdded_CreatesTrackedGame()
    {
        // Act
        var payload = new { userId = "new-user", appId = 888, addedAt = DateTimeOffset.UtcNow };
        var response = await Client.PostAsJsonAsync("/api/internal/wishlist-item-added", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify tracked game was created
        await WithDbContextAsync(async db =>
        {
            var tracked = (await db.TrackedGames.ToListAsync()).FirstOrDefault(tg => tg.AppId.Value == 888);
            tracked.Should().NotBeNull();
            tracked!.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task POST_internal_wishlistRemoved_DeactivatesTrackedGame()
    {
        // Arrange
        await WithDbContextAsync(async db =>
        {
            var trackedGame = TrackedGame.StartTracking(new SteamAppId(999), DateTimeOffset.UtcNow);
            await db.TrackedGames.AddAsync(trackedGame);
            await db.SaveChangesAsync();
        });

        // Act
        var payload = new { userId = "user", appId = 999, addedAt = DateTimeOffset.UtcNow };
        var response = await Client.PostAsJsonAsync("/api/internal/wishlist-item-removed", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await WithDbContextAsync(async db =>
        {
            var updated = (await db.TrackedGames.ToListAsync()).FirstOrDefault(tg => tg.AppId.Value == 999);
            updated.Should().NotBeNull();
            updated!.IsActive.Should().BeFalse();
        });
    }
}
