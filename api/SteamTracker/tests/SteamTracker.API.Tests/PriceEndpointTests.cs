using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SteamTracker.API.Models;
using SteamTracker.API.Tests.Helpers;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;
using SteamTracker.Application.Ports;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SteamTracker.API.Tests;

/// <summary>
/// Integration tests for GET /api/games/prices endpoint.
/// </summary>
[Collection("ApiIntegration")]
public class PriceEndpointTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public PriceEndpointTests(TestApiFactory factory)
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

    [Fact]
    public async Task GET_prices_returnsPricesForGivenAppIds()
    {
        // Arrange — seed two games with prices
        var appId1 = 100;
        var appId2 = 200;

        await _factory.SeedAsync(async sp =>
        {
            var gameRepo = sp.GetRequiredService<IGameRepository>();

            // Seed tracked games
            var trackedGameRepo = sp.GetRequiredService<ITrackedGameRepository>();
            var tg1 = TrackedGame.StartTracking(new SteamAppId(appId1), DateTimeOffset.UtcNow);
            var tg2 = TrackedGame.StartTracking(new SteamAppId(appId2), DateTimeOffset.UtcNow);
            await trackedGameRepo.SaveAsync(tg1);
            await trackedGameRepo.SaveAsync(tg2);

            // Seed games with prices
            var game1 = new Game(new SteamAppId(appId1));
            game1.ApplyPriceUpdate(new Money(29.99m, "EUR"), "Game Alpha", DateTimeOffset.UtcNow);
            await gameRepo.SaveAsync(game1);

            var game2 = new Game(new SteamAppId(appId2));
            game2.ApplyPriceUpdate(new Money(0m, "EUR"), "Free Game", DateTimeOffset.UtcNow);
            await gameRepo.SaveAsync(game2);
        });

        // Act
        var response = await Client.GetAsync($"/api/games/prices?appIds={appId1}&appIds={appId2}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GamePriceDto>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);

        result.Should().Contain(r => r.AppId == appId1);
        result.Should().Contain(r => r.AppId == appId2);
        var price1 = result.Single(r => r.AppId == appId1);
        price1.Amount.Should().Be(29.99m);
        price1.Currency.Should().Be("EUR");
        price1.IsUnavailable.Should().BeFalse();

        var price2 = result.Single(r => r.AppId == appId2);
        price2.Amount.Should().Be(0m);
        price2.Currency.Should().Be("EUR");
        price2.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task GET_prices_returnsEmptyArray_whenNoAppIdsProvided()
    {
        // Act
        var response = await Client.GetAsync("/api/games/prices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GamePriceDto>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_prices_returnsOnlyMatchingAppIds()
    {
        // Arrange — seed 3 games
        var appId1 = 300;
        var appId2 = 301;
        var appId3 = 302;

        await _factory.SeedAsync(async sp =>
        {
            var gameRepo = sp.GetRequiredService<IGameRepository>();
            var trackedGameRepo = sp.GetRequiredService<ITrackedGameRepository>();

            foreach (var appId in new[] { appId1, appId2, appId3 })
            {
                var tg = TrackedGame.StartTracking(new SteamAppId(appId), DateTimeOffset.UtcNow);
                await trackedGameRepo.SaveAsync(tg);
            }

            var game1 = new Game(new SteamAppId(appId1));
            game1.ApplyPriceUpdate(new Money(9.99m, "EUR"), "Game 1", DateTimeOffset.UtcNow);
            await gameRepo.SaveAsync(game1);

            var game2 = new Game(new SteamAppId(appId2));
            game2.ApplyPriceUpdate(new Money(19.99m, "EUR"), "Game 2", DateTimeOffset.UtcNow);
            await gameRepo.SaveAsync(game2);

            var game3 = new Game(new SteamAppId(appId3));
            game3.ApplyPriceUpdate(new Money(49.99m, "EUR"), "Game 3", DateTimeOffset.UtcNow);
            await gameRepo.SaveAsync(game3);
        });

        // Act — request only appId1 and appId3
        var response = await Client.GetAsync($"/api/games/prices?appIds={appId1}&appIds={appId3}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GamePriceDto>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result.Should().OnlyContain(r => r.AppId == appId1 || r.AppId == appId3);
        result.Should().NotContain(r => r.AppId == appId2);
    }

    [Fact]
    public async Task GET_prices_returnsUnavailableFlag()
    {
        // Arrange — seed a game marked unavailable
        var appId = 400;

        await _factory.SeedAsync(async sp =>
        {
            var trackedGameRepo = sp.GetRequiredService<ITrackedGameRepository>();
            var gameRepo = sp.GetRequiredService<IGameRepository>();

            var tg = TrackedGame.StartTracking(new SteamAppId(appId), DateTimeOffset.UtcNow);
            await trackedGameRepo.SaveAsync(tg);

            var game = new Game(new SteamAppId(appId));
            game.MarkUnavailable();
            await gameRepo.SaveAsync(game);
        });

        // Act
        var response = await Client.GetAsync($"/api/games/prices?appIds={appId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GamePriceDto>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
        result[0].IsUnavailable.Should().BeTrue();
        result[0].Amount.Should().BeNull();
    }
}
