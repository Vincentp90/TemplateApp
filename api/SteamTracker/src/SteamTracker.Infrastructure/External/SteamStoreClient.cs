using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Infrastructure.External;

public class SteamStoreClient : ISteamStoreClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenBucketRateLimiter _rateLimiter;
    private readonly ILogger<SteamStoreClient>? _logger;

    public SteamStoreClient(
        HttpClient httpClient,
        IConfiguration configuration,
        TokenBucketRateLimiter rateLimiter,
        ILogger<SteamStoreClient>? logger = null)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<(Money? Price, string Name, bool IsUnavailable)> FetchPriceAsync(int appId)
    {
        using var lease = await _rateLimiter.AcquireAsync();

        var uri = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=price_overview&cc=de&l=german";
        var response = await _httpClient.GetAsync(uri);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new SteamRateLimitException("Rate limited by Steam API.");

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Steam API returned {StatusCode} for appId={AppId}", response.StatusCode, appId);
            return (null, string.Empty, false);
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty(appId.ToString(), out var appElement) ||
            !appElement.GetProperty("success").GetBoolean())
        {
            _logger?.LogWarning("Game not found on Steam for appId={AppId}", appId);
            return (null, string.Empty, true);
        }

        var data = appElement.GetProperty("data");

        // Empty data array means the app exists but has no price data — treat as free game
        // (e.g. CS2 / app 730 with filters=price_overview)
        if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() == 0)
        {
            _logger?.LogInformation("Free game detected for appId={AppId}", appId);
            return (Money.Free, string.Empty, false);
        }

        if (data.ValueKind != JsonValueKind.Object)
        {
            _logger?.LogInformation("Unexpected data structure for appId={AppId}", appId);
            return (null, string.Empty, false);
        }

        var name = string.Empty;
        if (data.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
            name = nameProp.GetString() ?? string.Empty;

        if (data.TryGetProperty("is_free", out var isFreeProp) && isFreeProp.GetBoolean())
        {
            _logger?.LogInformation("Free game detected for appId={AppId}: {Name}", appId, name);
            return (Money.Free, name, false);
        }

        if (data.TryGetProperty("price_overview", out var priceOverview))
        {
            if (priceOverview.ValueKind != JsonValueKind.Object)
            {
                _logger?.LogInformation("price_overview is not an object for appId={AppId}", appId);
                return (null, string.Empty, false);
            }

            if (!priceOverview.TryGetProperty("final", out var finalProp) || finalProp.ValueKind != JsonValueKind.Number)
            {
                _logger?.LogInformation("Missing or invalid 'final' price for appId={AppId}", appId);
                return (null, string.Empty, false);
            }

            if (!priceOverview.TryGetProperty("currency", out var currencyProp) || currencyProp.ValueKind != JsonValueKind.String)
            {
                _logger?.LogInformation("Missing or invalid 'currency' for appId={AppId}", appId);
                return (null, string.Empty, false);
            }

            var amount = finalProp.GetInt32() / 100m;
            var currency = currencyProp.GetString() ?? "EUR";
            _logger?.LogInformation("Price for {Name} (appId={AppId}): {Amount} {Currency}", name, appId, amount, currency);
            return (new Money(amount, currency), name, false);
        }

        _logger?.LogInformation("No price data available for appId={AppId}", appId);
        return (null, string.Empty, false);
    }
}

public class SteamRateLimitException : Exception
{
    public SteamRateLimitException(string message) : base(message) { }
}
