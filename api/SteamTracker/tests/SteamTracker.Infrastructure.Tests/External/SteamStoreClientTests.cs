using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SteamTracker.Infrastructure.External;
using System.Net;
using System.Threading.RateLimiting;

namespace SteamTracker.Infrastructure.Tests.External;

public class SteamStoreClientTests
{
    private static SteamStoreClient CreateClient(HttpMessageHandler handler, TokenBucketRateLimiter? rateLimiter = null)
    {
        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        rateLimiter ??= new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 100,
            TokenLimit = 100,
            TokensPerPeriod = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true
        });

        return new SteamStoreClient(httpClient, rateLimiter, NullLogger<SteamStoreClient>.Instance);
    }

    [Fact]
    public async Task FetchPriceAsync_RateLimitThrottlesSubsequentCalls()
    {
        // Arrange — a handler that returns a successful response for any app ID
        var handler = new FakeHttpMessageHandler();
        handler.SetResponseForAnyApp();
        var client = CreateClient(handler);

        // Act — fire two calls; the second should simply wait for a token rather than fail
        var firstCall = client.FetchPriceAsync(12345);
        var secondCall = client.FetchPriceAsync(12346);
        await Task.WhenAll(firstCall, secondCall);

        // Assert — both succeed, throttling only adds delay
        (await firstCall).Should().NotBeNull();
        (await secondCall).Should().NotBeNull();
    }

    [Fact]
    public async Task FetchPriceAsync_LimiterRejectsWhenQueueIsFull_ThrowsSteamRateLimitException()
    {
        // Arrange — a limiter with no capacity and no queue room: the very first
        // acquire attempt fails immediately.
        var handler = new FakeHttpMessageHandler();
        handler.SetResponseForAnyApp();

        var exhaustedLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            QueueLimit = 0,
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromMinutes(5),
            AutoReplenishment = false // no tokens will ever replenish during the test
        });
        // Consume the only token up front so the client's call has nothing to acquire.
        exhaustedLimiter.AttemptAcquire();

        var client = CreateClient(handler, exhaustedLimiter);

        // Act
        var act = () => client.FetchPriceAsync(12345);

        // Assert
        await act.Should().ThrowAsync<SteamRateLimitException>();
    }

    [Fact]
    public async Task FetchPriceAsync_429Response_ThrowsSteamRateLimitException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(HttpStatusCode.TooManyRequests);
        var client = CreateClient(handler);

        // Act
        var act = () => client.FetchPriceAsync(12345);

        // Assert
        await act.Should().ThrowAsync<SteamRateLimitException>();
    }

    [Fact]
    public async Task FetchPriceAsync_NonSuccessStatusCode_ReturnsNull()
    {
        // Arrange — any other error (500, 503, ...) is treated as transient, not thrown
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(HttpStatusCode.ServiceUnavailable);
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(12345);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchPriceAsync_MalformedJson_ReturnsNullInsteadOfThrowing()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse("{not valid json");
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(12345);

        // Assert
        result.Should().BeNull();
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
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(12345);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().NotBeNull();
        result.Price!.Value.Amount.Should().Be(19.99m);
        result.Price.Value.Currency.Value.Should().Be("EUR");
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
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(220);

        // Assert — should still extract price even when name/is_free are missing
        result.Should().NotBeNull();
        result!.Price.Should().NotBeNull();
        result.Price!.Value.Amount.Should().Be(1.95m);
        result.Price.Value.Currency.Value.Should().Be("EUR");
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
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(54321);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().NotBeNull();
        result.Price!.Value.IsFree.Should().BeTrue();
        result.Name.Should().Be("Free Game");
        result.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task FetchPriceAsync_EmptyDataArray_ReturnsFreeMoney()
    {
        // Arrange — Steam returns empty data array for free games like CS2 (app 730)
        // when using filters=price_overview. success is true but data is [].
        var json = """
        {
            "730": {
                "success": true,
                "data": []
            }
        }
        """;
        var handler = new FakeHttpMessageHandler();
        handler.SetResponse(json);
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(730);

        // Assert — should be treated as a free game so the use case can persist it
        result.Should().NotBeNull();
        result!.Price.Should().NotBeNull();
        result.Price!.Value.IsFree.Should().BeTrue();
        result.Price.Value.Amount.Should().Be(0m);
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
        var client = CreateClient(handler);

        // Act
        var result = await client.FetchPriceAsync(362003);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().BeNull();
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

        var query = request.RequestUri?.Query ?? string.Empty;
        var appId = int.Parse(query.TrimStart('?').Split('&')[0].Replace("appids=", ""));

        var json = BuildAppResponse(appId);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });
    }

    private static string BuildAppResponse(int appId) =>
        "{\"" + appId + "\":{\"success\":true,\"data\":{\"name\":\"Any Game\",\"is_free\":false,\"price_overview\":{\"final\":999,\"currency\":\"EUR\"}}}}";
}