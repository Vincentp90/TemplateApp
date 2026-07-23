using Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tests.IntegrationTests;

/// <summary>
/// DTO for deserializing prices passthrough responses in tests.
/// </summary>
internal record PricesPassthroughDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable);

/// <summary>
/// Integration tests for the /api/prices passthrough endpoint (auth required).
/// Uses a mock HTTP handler to capture requests sent to SteamTracker.
/// </summary>
public class PricesPassthroughTests : IAsyncLifetime
{
    private HttpClient _client = null!;
    private CapturingHandler _capturingHandler = null!;
    private PricesFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        _capturingHandler = new CapturingHandler();
        _factory = new PricesFactory(_capturingHandler);
        await _factory.InitializeAsync();
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetPrices_forwardsToSteamTracker_and_returns_response()
    {
        // Arrange — pre-seed the mock to return a price
        _capturingHandler.PrepareResponse(new List<PricesPassthroughDto>
        {
            new PricesPassthroughDto(42, 19.99m, "EUR", DateTimeOffset.Parse("2025-07-01T12:00:00Z"), false)
        });

        // Act — authenticated request
        var response = await _client.GetAsync("/api/prices?appIds=42", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items = JsonSerializer.Deserialize<List<PricesPassthroughDto>>(body, options);
        items.Should().NotBeNull();
        items!.Count.Should().Be(1);
        items[0].AppId.Should().Be(42);
        items[0].Amount.Should().Be(19.99m);
        items[0].Currency.Should().Be("EUR");

        // Verify the passthrough forwarded to the correct SteamTracker endpoint
        _capturingHandler.LastRequest.Should().NotBeNull();
        _capturingHandler.LastRequest!.RequestUri!.ToString().Should().Contain("/api/games/prices");
        _capturingHandler.LastRequest.RequestUri.ToString().Should().Contain("appIds=42");
    }

    [Fact]
    public async Task GetPrices_forwards_appIds_query_params_correctly()
    {
        // Arrange
        _capturingHandler.PrepareResponse(new List<PricesPassthroughDto>
        {
            new PricesPassthroughDto(100, 29.99m, "EUR", DateTimeOffset.Parse("2025-07-01T12:00:00Z"), false),
            new PricesPassthroughDto(200, 9.99m, "EUR", DateTimeOffset.Parse("2025-07-01T12:00:00Z"), false)
        });

        // Act — multiple appIds
        var response = await _client.GetAsync("/api/prices?appIds=100&appIds=200", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify the passthrough forwarded all appIds
        _capturingHandler.LastRequest.Should().NotBeNull();
        _capturingHandler.LastRequest!.RequestUri!.ToString().Should().Contain("appIds=100");
        _capturingHandler.LastRequest.RequestUri.ToString().Should().Contain("appIds=200");
    }

    [Fact]
    public async Task GetPrices_returns_empty_when_steamtracker_returns_empty()
    {
        // Arrange — mock returns 200 with empty array
        _capturingHandler.PrepareResponse(new List<PricesPassthroughDto>());

        // Act
        var response = await _client.GetAsync("/api/prices?appIds=9999", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var items = JsonSerializer.Deserialize<List<PricesPassthroughDto>>(body);
        items.Should().NotBeNull();
        items!.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPrices_returns_empty_when_no_appIds_provided()
    {
        // Act — no appIds query param
        var response = await _client.GetAsync("/api/prices", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var items = JsonSerializer.Deserialize<List<PricesPassthroughDto>>(body);
        items.Should().NotBeNull();
        items!.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetPrices_returns_Unauthorized_when_no_auth()
    {
        // Arrange — create an unauthenticated client
        var unauthFactory = new PricesFactory(new CapturingHandler());
        await unauthFactory.InitializeAsync();
        var unauthClient = unauthFactory.CreateClient();

        try
        {
            // Act
            var response = await unauthClient.GetAsync("/api/prices?appIds=42", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
        finally
        {
            await unauthFactory.DisposeAsync();
        }
    }

    /// <summary>
    /// Captures the last HTTP request sent by the passthrough.
    /// </summary>
    private class CapturingHandler : DelegatingHandler
    {
        private List<PricesPassthroughDto>? _preseededResponse;

        public HttpRequestMessage? LastRequest { get; private set; }

        public void PrepareResponse(List<PricesPassthroughDto> data)
        {
            _preseededResponse = data;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (_preseededResponse != null)
            {
                var json = JsonSerializer.Serialize(_preseededResponse);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    /// <summary>
    /// WebApplicationFactory that injects a custom HTTP handler for the SteamTracker passthrough.
    /// </summary>
    private class PricesFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly CapturingHandler _handler;
        private bool _initialized;

        public PricesFactory(CapturingHandler handler)
        {
            _handler = handler;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove hosted services
                var hostedServices = services
                    .Where(d => d.ServiceType == typeof(IHostedService))
                    .ToList();
                foreach (var svc in hostedServices)
                    services.Remove(svc);

                // Mock the SteamTracker proxy — we test the passthrough with HTTP handler mocking
                var proxyDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ISteamTrackerAlertProxy));
                if (proxyDescriptor != null)
                    services.Remove(proxyDescriptor);
                var proxyMock = new Moq.Mock<ISteamTrackerAlertProxy>();
                services.AddScoped(_ => proxyMock.Object);

                // Replace the default HttpClient with our capturing handler
                var httpClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(HttpClient));
                if (httpClientDescriptor != null)
                    services.Remove(httpClientDescriptor);

                // Configure HttpClient with SteamTracker base address and our capturing handler
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "SteamTrackerUri", "http://mock" },
                        { "Jwt:Key", "MuF2rOUXJnC8/rGtoB0sfXnjWWmlgu63AqfqoPUqNxw=" },
                        { "Jwt:Issuer", "WishlistApp" },
                        { "Jwt:Audience", "WishlistApp_audience" }
                    })
                    .Build();
                services.AddSingleton<IConfiguration>(config);

                services.AddHttpClient("SteamTracker", client =>
                {
                    client.BaseAddress = new Uri("http://mock");
                })
                .ConfigurePrimaryHttpMessageHandler(() => _handler);
            });
        }

        public async ValueTask InitializeAsync()
        {
            if (!_initialized)
            {
                await Task.CompletedTask;
                _initialized = true;
            }
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var client = CreateClient();

            var username = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            var credentials = new { Username = username, Password = password };

            await client.PostAsJsonAsync("/auth/register", credentials);

            var loginResponse = await client.PostAsJsonAsync("/auth/login", credentials);
            loginResponse.EnsureSuccessStatusCode();

            // Because dev/unit-test environment has no SSL, we need to manually set the cookie.
            var cookie = loginResponse.Headers
                .GetValues("Set-Cookie")
                .First(c => c.StartsWith("auth_token"));
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            return client;
        }
    }
}
