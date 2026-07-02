using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Infrastructure.External;

public class SteamStoreClient : ISteamStoreClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUri;

    public SteamStoreClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Steam:ApiKey"] ?? string.Empty;
        _baseUri = configuration["Steam:BaseUri"] ?? "https://store.steampowered.com";
    }

    public async Task<(Money Price, string Name)?> FetchPriceAsync(int appId)
    {
        // Use Steam API /api/featured-categories/v2 or price API
        // For simplicity, we'll use the store page and parse, or the API
        var uri = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=de&l=german";

        var response = await _httpClient.GetAsync(uri);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new SteamRateLimitException("Rate limited by Steam API.");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty(appId.ToString(), out var appElement) ||
            !appElement.GetProperty("success").GetBoolean())
            return null;

        var data = appElement.GetProperty("data");
        var name = data.GetProperty("name").GetString() ?? string.Empty;

        // Check if the game has a free package
        if (data.TryGetProperty("is_free", out var isFreeProp) && isFreeProp.GetBoolean())
            return (Money.Free, name);

        // Get price summary
        if (data.TryGetProperty("price_overview", out var priceOverview))
        {
            var amount = priceOverview.GetProperty("final").GetInt32() / 100m;
            var currency = priceOverview.GetProperty("currency").GetString() ?? "EUR";
            return (new Money(amount, currency), name);
        }

        return null;
    }
}

public class SteamRateLimitException : Exception
{
    public SteamRateLimitException(string message) : base(message) { }
}
