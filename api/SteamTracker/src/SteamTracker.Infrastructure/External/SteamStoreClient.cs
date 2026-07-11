using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Infrastructure.External;

public class SteamStoreClient : ISteamStoreClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenBucketRateLimiter _rateLimiter;
    private readonly ILogger<SteamStoreClient> _logger;

    public SteamStoreClient(
        HttpClient httpClient,
        TokenBucketRateLimiter rateLimiter,
        ILogger<SteamStoreClient> logger)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<SteamPriceResult?> FetchPriceAsync(int appId, CancellationToken cancellationToken = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(cancellationToken: cancellationToken);
        if (!lease.IsAcquired)
            throw new SteamRateLimitException($"Local rate limiter rejected request for appId={appId}.");

        var uri = $"https://store.steampowered.com/api/appdetails?appids={appId}&filters=price_overview&cc=de&l=german";
        var response = await _httpClient.GetAsync(uri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new SteamRateLimitException("Rate limited by Steam API.");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Steam API returned {StatusCode} for appId={AppId}", response.StatusCode, appId);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Steam response for appId={AppId}", appId);
            return null;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty(appId.ToString(), out var appElement) ||
                !appElement.TryGetProperty("success", out var successProp) ||
                (successProp.ValueKind != JsonValueKind.True && successProp.ValueKind != JsonValueKind.False))
            {
                _logger.LogWarning("Unexpected response shape for appId={AppId}", appId);
                return null;
            }

            if (!successProp.GetBoolean())
            {
                _logger.LogInformation("Game unavailable on Steam for appId={AppId}", appId);
                return SteamPriceResult.Unavailable();
            }

            if (!appElement.TryGetProperty("data", out var data))
            {
                _logger.LogInformation("No data field for appId={AppId}", appId);
                return null;
            }

            // Empty data array means the app exists but has no price data — this is how
            // F2P games (e.g. CS2 / app 730) respond when filters=price_overview is applied.
            if (data.ValueKind == JsonValueKind.Array)
            {
                _logger.LogInformation("Free game detected (empty data array) for appId={AppId}", appId);
                return SteamPriceResult.Free(name: string.Empty);
            }

            if (data.ValueKind != JsonValueKind.Object)
            {
                _logger.LogInformation("Unexpected data structure for appId={AppId}", appId);
                return null;
            }

            var name = data.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString() ?? string.Empty
                : string.Empty;

            if (data.TryGetProperty("is_free", out var isFreeProp) && isFreeProp.ValueKind == JsonValueKind.True)
            {
                _logger.LogInformation("Free game detected for appId={AppId}: {Name}", appId, name);
                return SteamPriceResult.Free(name);
            }

            if (!data.TryGetProperty("price_overview", out var priceOverview) ||
                priceOverview.ValueKind != JsonValueKind.Object)
            {
                _logger.LogInformation("No price data available for appId={AppId}", appId);
                return null;
            }

            if (!priceOverview.TryGetProperty("final", out var finalProp) || finalProp.ValueKind != JsonValueKind.Number ||
                !priceOverview.TryGetProperty("currency", out var currencyProp) || currencyProp.ValueKind != JsonValueKind.String)
            {
                _logger.LogInformation("Malformed price_overview for appId={AppId}", appId);
                return null;
            }

            var amount = finalProp.GetInt32() / 100m;
            var currency = currencyProp.GetString() ?? "EUR";

            _logger.LogInformation("Price for {Name} (appId={AppId}): {Amount} {Currency}", name, appId, amount, currency);
            return SteamPriceResult.WithPrice(new Money(amount, currency), name);
        }
    }
}