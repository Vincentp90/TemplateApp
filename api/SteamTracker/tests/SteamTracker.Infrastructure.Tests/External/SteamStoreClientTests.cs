using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using SteamTracker.Infrastructure.External;
using System.Net;
using System.Threading.RateLimiting;

namespace SteamTracker.Infrastructure.Tests.External;

public class SteamStoreClientTests
{
    [Fact]
    public async Task FetchPriceAsync_RateLimitThrottlesSubsequentCalls()
    {
        // Arrange — a handler that returns a successful response for any request
        var handler = new FakeHttpMessageHandler();
        handler.SetResponseForAnyApp();

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        var config = new Dictionary<string, string?> { ["Steam:ApiKey"] = "test-key" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });
        var client = new SteamStoreClient(httpClient, configuration, rateLimiter);

        // First call should succeed
        var firstCall = client.FetchPriceAsync(12345);

        // Second call should be throttled — it should wait while the limiter is exhausted
        var secondCall = client.FetchPriceAsync(12346);

        // Wait for both to complete
        await Task.WhenAll(firstCall, secondCall);

        // Both should have succeeded (the throttling just adds delay, doesn't fail)
        (await firstCall).Should().NotBeNull();
        (await secondCall).Should().NotBeNull();
    }

    [Fact]
    public async Task FetchPriceAsync_429Response_ThrowsSteamRateLimitException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(HttpStatusCode.TooManyRequests);

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        var config = new Dictionary<string, string?> { ["Steam:ApiKey"] = "test-key" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });
        var client = new SteamStoreClient(httpClient, configuration, rateLimiter);

        // Act
        var act = () => client.FetchPriceAsync(12345);

        // Assert
        await act.Should().ThrowAsync<SteamRateLimitException>();
    }

    [Fact]
    public async Task FetchPriceAsync_SuccessfulResponse_ReturnsPriceAndName()
    {
        // Arrange
        var json = """
        {
            "12345": {
                "success": true,
                "data": {
                    "name": "Test Game",
                    "is_free": false,
                    "price_overview": {
                        "final": 1999,
                        "currency": "EUR"
                    }
                }
            }
        }
        """;
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(json);

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        var config = new Dictionary<string, string?> { ["Steam:ApiKey"] = "test-key" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });
        var client = new SteamStoreClient(httpClient, configuration, rateLimiter);

        // Act
        var result = await client.FetchPriceAsync(12345);

        // Assert
        result.Should().NotBeNull();
        var price = result!.Price;
        price.Should().NotBeNull();
        price.Value.Amount.Should().Be(19.99m);
        price.Value.Currency.Value.Should().Be("EUR");
        result.Name.Should().Be("Test Game");
        result.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task FetchPriceAsync_PartialDataWithoutName_ReturnsPrice()
    {
        // Arrange — Steam's filters=price_overview strips name/is_free from data.
        // This is exactly what the real Steam API returns for apps like Half-Life 2.
        var json = """
        {
            "220": {
                "success": true,
                "data": {
                    "price_overview": {
                        "final": 195,
                        "currency": "EUR"
                    }
                }
            }
        }
        """;
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(json);

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        var config = new Dictionary<string, string?> { ["Steam:ApiKey"] = "test-key" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });
        var client = new SteamStoreClient(httpClient, configuration, rateLimiter);

        // Act
        var result = await client.FetchPriceAsync(220);

        // Assert — should still extract price even when name/is_free are missing
        result.Should().NotBeNull();
        var price = result!.Price;
        price.Should().NotBeNull();
        price.Value.Amount.Should().Be(1.95m);
        price.Value.Currency.Value.Should().Be("EUR");
        result.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task FetchPriceAsync_FreeGame_ReturnsFreeMoney()
    {
        // Arrange
        var json = """
        {
            "54321": {
                "success": true,
                "data": {
                    "name": "Free Game",
                    "is_free": true
                }
            }
        }
        """;
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(json);

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        var config = new Dictionary<string, string?> { ["Steam:ApiKey"] = "test-key" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });
        var client = new SteamStoreClient(httpClient, configuration, rateLimiter);

        // Act
        var result = await client.FetchPriceAsync(54321);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().NotBeNull();
        result.Price!.Value.IsFree.Should().BeTrue();
        result.Name.Should().Be("Free Game");
        result.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task FetchPriceAsync_UnavailableGame_ReturnsUnavailableFlag()
    {
        // Arrange — success: false means the game no longer exists on Steam
        var json = """
        {
            "362003": {
                "success": false
            }
        }
        """;
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(json);

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        var config = new Dictionary<string, string?> { ["Steam:ApiKey"] = "test-key" };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 1,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });
        var client = new SteamStoreClient(httpClient, configuration, rateLimiter);

        // Act
        var result = await client.FetchPriceAsync(362003);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().BeNull();
        result.Name.Should().BeEmpty();
        result.IsUnavailable.Should().BeTrue();
    }
}

/// <summary>
/// A fake HTTP message handler that returns a configurable response.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private string? _fixedJson;
    private HttpStatusCode? _statusCode;

    public FakeHttpMessageHandler()
    {
        // Default: successful response for app 12345
        SetResponse("""{"12345":{"success":true,"data":{"name":"Default","is_free":false,"price_overview":{"final":999,"currency":"EUR"}}}}""");
    }

    public void SetResponse(string json)
    {
        _fixedJson = json;
        _statusCode = null;
    }

    public void SetResponse(HttpStatusCode statusCode)
    {
        _fixedJson = null;
        _statusCode = statusCode;
    }

    public void SetResponseForAnyApp()
    {
        // Reset so dynamic mode is active
        _fixedJson = null;
        _statusCode = null;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_fixedJson is not null)
        {
            var response = new HttpResponseMessage(_statusCode ?? HttpStatusCode.OK)
            {
                Content = new StringContent(_fixedJson)
            };
            return Task.FromResult(response);
        }

        if (_statusCode.HasValue)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode.Value)
            {
                Content = new StringContent("{}")
            });
        }

        // Dynamic mode: extract app ID from query string and build a response
        var query = request.RequestUri?.Query ?? string.Empty;
        var appId = int.Parse(query.Split('&')[0].Replace("?", "").Replace("appids=", ""));

        var json = BuildAppResponse(appId);
        var dynamicResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        return Task.FromResult(dynamicResponse);
    }

    private static string BuildAppResponse(int appId)
    {
        return "{\"" + appId + "\":{\"success\":true,\"data\":{\"name\":\"Any Game\",\"is_free\":false,\"price_overview\":{\"final\":999,\"currency\":\"EUR\"}}}}";
    }
}
