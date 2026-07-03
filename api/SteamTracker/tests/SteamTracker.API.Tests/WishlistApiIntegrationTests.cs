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

namespace SteamTracker.API.Tests;

/// <summary>
/// Integration tests for SteamTracker API endpoints using WebApplicationFactory + testcontainers.
/// </summary>
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
        // Act
        var response = await Client.GetAsync("/api/wishlist?userId=test-user");

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
        var response = await Client.GetAsync("/api/wishlist?userId=unknown-user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_alertRule_CreatesRule()
    {
        // Act
        var response = await Client.PostAsync("/api/wishlist/test-user/games/42/alert?thresholdAmount=15.0&currency=EUR", null);

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
        // Act — app 99999 is not tracked
        var response = await Client.PostAsync("/api/wishlist/test-user/games/99999/alert?thresholdAmount=10.0", null);

        // Assert — InvalidOperationException → 500
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DELETE_alertRule_DeletesRule()
    {
        // Arrange — create a rule first
        await Client.PostAsync("/api/wishlist/test-user/games/42/alert?thresholdAmount=10.0&currency=EUR", null);

        Guid? ruleId = null;
        await WithDbContextAsync(async db =>
        {
            var rules = await db.AlertRules.ToListAsync();
            var rule = rules.First(r => r.UserId == "test-user" && r.AppId.Value == 42);
            ruleId = rule.AlertRuleId;
        });

        // Act
        var response = await Client.DeleteAsync($"/api/wishlist/test-user/alert/{ruleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await WithDbContextAsync(async db =>
        {
            var rule = await db.AlertRules.FindAsync(ruleId);
            rule.Should().BeNull();
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
