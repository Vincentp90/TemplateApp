using FluentAssertions;
using System.Net.Http.Json;
using Application.Contracts;

namespace CrossService.Tests;

/// <summary>
/// Cross-service integration test covering the full flow:
///   1. Register a user in WishlistApi
///   2. Add an item to the wishlist
///   3. Retrieve the wishlist with associated pricedata
/// </summary>
public class CrossServiceTests : IAsyncLifetime
{
    private static readonly Lazy<CrossServiceFixture> _fixture = new(() => new CrossServiceFixture());
    private CrossServiceFixture Fixture => _fixture.Value;

    public Task InitializeAsync() => Fixture.InitializeAsync();
    public Task DisposeAsync() => Fixture.DisposeAsync();

    /// <summary>
    /// Full cross-service flow: add a wishlist item and retrieve it with price data.
    /// </summary>
    [Fact]
    public async Task AddWishlistItemAndGetPrices_CrossServiceFlow_Works()
    {
        // ===== ARRANGE =====
        const int appId = 12345;
        var gameName = "Cross Service Test Game";
        var priceAmount = 29.99m;

        // Seed app listing in WishlistApi DB (FK constraint requires it)
        await Fixture.SeedWishlistAppListingAsync(appId, gameName);

        // Seed a tracked game and price in SteamTracker's database
        await Fixture.SeedSteamTrackerGameAsync(appId, gameName, priceAmount);

        // Act 1: Add item to wishlist via WishlistApi
        var addResponse = await Fixture.WishlistClient.PostAsync($"/wishlist/{appId}", null);
        addResponse.EnsureSuccessStatusCode();

        // Act 2: Retrieve wishlist
        var wishlistResponse = await Fixture.WishlistClient.GetAsync("/wishlist");
        wishlistResponse.EnsureSuccessStatusCode();

        // ===== ASSERT: Wishlist contains the item =====
        var wishlist = await wishlistResponse.Content.ReadFromJsonAsync<Wishlist>();
        wishlist.Should().NotBeNull();
        wishlist!.Items.Should().ContainSingle();
        var wishlistItem = wishlist.Items.First();
        wishlistItem.AppId.Should().Be(appId);
        wishlistItem.Name.Should().Be(gameName);
        wishlistItem.DateAdded.Should().NotBeNull();

        // Act 3: Retrieve prices from SteamTracker (mocked)
        var pricesResponse = await Fixture.WishlistClient.GetAsync($"/api/prices?appIds={appId}");
        pricesResponse.EnsureSuccessStatusCode();

        // ===== ASSERT: Prices endpoint returns the seeded data =====
        var prices = await pricesResponse.Content.ReadFromJsonAsync<IEnumerable<GamePriceDto>>();
        prices.Should().NotBeNull();
        prices.Should().ContainSingle();
        var price = prices!.First();
        price.AppId.Should().Be(appId);
        price.Amount.Should().Be(priceAmount);
        price.Currency.Should().Be("EUR");
        price.IsUnavailable.Should().BeFalse();
        price.LastCheckedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that SteamTracker price data is correctly returned when queried via WishlistApi passthrough.
    /// </summary>
    [Fact]
    public async Task MultipleGamesAndPrices_CrossServiceFlow_ReturnsCorrectData()
    {
        // ===== ARRANGE =====
        var games = new[]
        {
            (AppId: 11111, Name: "Game A", Price: 9.99m),
            (AppId: 22222, Name: "Game B", Price: 19.99m),
            (AppId: 33333, Name: "Game C", Price: 49.99m),
        };

        // Seed app listings in WishlistApi DB (FK constraint requires it)
        foreach (var game in games)
        {
            await Fixture.SeedWishlistAppListingAsync(game.AppId, game.Name);
        }

        // Seed tracked games and prices in SteamTracker's database
        foreach (var game in games)
        {
            await Fixture.SeedSteamTrackerGameAsync(game.AppId, game.Name, game.Price);
        }

        // Add two items to wishlist
        await Fixture.WishlistClient.PostAsync($"/wishlist/{games[0].AppId}", null);
        await Fixture.WishlistClient.PostAsync($"/wishlist/{games[1].AppId}", null);

        // Act: Get prices for all three games
        var pricesResponse = await Fixture.WishlistClient.GetAsync(
            $"/api/prices?appIds={games[0].AppId}&appIds={games[1].AppId}&appIds={games[2].AppId}");
        pricesResponse.EnsureSuccessStatusCode();

        // Assert: All three prices returned
        var prices = await pricesResponse.Content.ReadFromJsonAsync<IEnumerable<GamePriceDto>>();
        prices.Should().NotBeNull();
        prices.Should().HaveCount(3);

        var priceDict = prices!.ToDictionary(p => (int)p.AppId!);
        priceDict.Should().ContainKey((int)games[0].AppId);
        priceDict.Should().ContainKey((int)games[1].AppId);
        priceDict.Should().ContainKey((int)games[2].AppId);

        priceDict[(int)games[0].AppId]!.Amount.Should().Be((decimal?)games[0].Price);
        priceDict[(int)games[1].AppId]!.Amount.Should().Be((decimal?)games[1].Price);
        priceDict[(int)games[2].AppId]!.Amount.Should().Be((decimal?)games[2].Price);
    }
}
