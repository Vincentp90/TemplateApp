using Application.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;

namespace Tests.IntegrationTests;

/// <summary>
/// Integration tests for the WishlistApi proxy alert endpoints.
/// Uses a mock HTTP handler to capture requests sent to SteamTracker.
/// No database, containers, or seeding — just verifies proxy HTTP construction.
/// </summary>
public class AlertProxyEndpointTests : IAsyncLifetime
{
    private HttpClient _client = null!;
    private CapturingHandler _capturingHandler = null!;

    public async ValueTask InitializeAsync()
    {
        _capturingHandler = new CapturingHandler();
        var factory = new ProxyTestFactory(_capturingHandler);
        await factory.InitializeAsync();

        _client = await factory.CreateAuthenticatedClientAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
    }

    #region POST /wishlist/{appId}/alert

    [Fact]
    public async Task SetAlertAsync_forwardsCorrectRequestToSteamTracker()
    {
        // Arrange
        const int appId = 42;

        // Act
        var response = await _client.PostAsync($"/wishlist/{appId}/alert?thresholdAmount=15.50&currency=EUR", null, TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify the proxy forwarded the correct request
        _capturingHandler.LastRequest.Should().NotBeNull();
        _capturingHandler.LastRequest!.Method.Should().Be(System.Net.Http.HttpMethod.Post);
        _capturingHandler.LastRequest.RequestUri!.ToString().Should().Contain("/api/games/42/alert");
        _capturingHandler.LastRequest.RequestUri.ToString().Should().Contain("thresholdAmount=15.5");
        _capturingHandler.LastRequest.RequestUri.ToString().Should().Contain("currency=EUR");
        _capturingHandler.LastRequest!.Headers.Contains("X-Internal-UserId").Should().BeTrue();
    }

    [Fact]
    public async Task SetAlertAsync_defaultCurrencyIsEuri()
    {
        // Arrange
        const int appId = 100;

        // Act — omit currency query param to test default
        var response = await _client.PostAsync($"/wishlist/{appId}/alert?thresholdAmount=20", null, TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // The proxy should include currency=EUR even when not specified
        _capturingHandler.LastRequest!.RequestUri!.ToString().Should().Contain("currency=EUR");
    }

    [Fact]
    public async Task SetAlertAsync_invalidAppId_doesNotThrow()
    {
        // Arrange — app 9999 doesn't exist in tracked_games, but proxy doesn't validate
        // (SteamTracker would handle that)

        // Act
        var response = await _client.PostAsync("/wishlist/9999/alert?thresholdAmount=10&currency=EUR", null, TestContext.Current.CancellationToken);

        // Assert — proxy returns whatever SteamTracker returns (which is mocked)
        // The proxy mock returns success, so we get 200
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region DELETE /wishlist/{alertRuleId}/alert

    [Fact]
    public async Task DeleteAlertAsync_forwardsCorrectRequestToSteamTracker()
    {
        // Arrange
        var alertRuleId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

        // Act
        var response = await _client.DeleteAsync($"/wishlist/{alertRuleId}/alert", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify the proxy forwarded the correct DELETE request
        _capturingHandler.LastRequest.Should().NotBeNull();
        _capturingHandler.LastRequest!.Method.Should().Be(System.Net.Http.HttpMethod.Delete);
        _capturingHandler.LastRequest.RequestUri!.ToString()
            .Should().Be($"http://mock/api/alert/{alertRuleId}");
        _capturingHandler.LastRequest!.Headers.Contains("X-Internal-UserId").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAlertAsync_invalidGuid_doesNotThrow()
    {
        // Arrange — invalid GUID but proxy doesn't validate (SteamTracker would)

        // Act
        var response = await _client.DeleteAsync("/wishlist/00000000-0000-0000-0000-000000000000/alert", TestContext.Current.CancellationToken);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    /// <summary>
    /// Captures the last HTTP request sent by the proxy.
    /// </summary>
    private class CapturingHandler : DelegatingHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            // Return a successful response — we're testing the proxy's forwarding, not SteamTracker
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// WebApplicationFactory that replaces ISteamTrackerAlertProxy with a capturing HTTP handler.
    /// No database, containers, or seeding — just verifies proxy HTTP construction.
    /// </summary>
    private class ProxyTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly CapturingHandler _handler;
        private bool _initialized;

        public ProxyTestFactory(CapturingHandler handler)
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

                // Replace IConfiguration so the proxy uses http://mock instead of the real SteamTrackerUri
                var configDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConfiguration));
                if (configDescriptor != null)
                    services.Remove(configDescriptor);
                var mockConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "SteamTrackerUri", "http://mock" },
                        { "Jwt:Key", "MuF2rOUXJnC8/rGtoB0sfXnjWWmlgu63AqfqoPUqNxw=" },
                        { "Jwt:Issuer", "WishlistApp" },
                        { "Jwt:Audience", "WishlistApp_audience" }
                    })
                    .Build();
                services.AddSingleton<IConfiguration>(mockConfig);

                // Replace SteamTracker proxy with our capturing handler
                var proxyDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ISteamTrackerAlertProxy));
                if (proxyDescriptor != null)
                    services.Remove(proxyDescriptor);

                // Remove the default HttpClient used by the proxy
                var httpClientDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(HttpClient));
                if (httpClientDescriptor != null)
                    services.Remove(httpClientDescriptor);

                services.AddHttpClient<ISteamTrackerAlertProxy, Infrastructure.SharedDb.SteamTrackerAlertProxy>(client =>
                {
                    client.BaseAddress = new Uri("http://mock/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => _handler);
            });
        }

        public async ValueTask InitializeAsync()
        {
            if (!_initialized)
            {
                _initialized = true;
            }
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync()
        {
            var client = CreateClient();
            var username = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            await client.PostAsJsonAsync("/auth/register", new { username, password });

            var loginResponse = await client.PostAsJsonAsync("/auth/login", new { username, password });
            loginResponse.EnsureSuccessStatusCode();

            var cookie = loginResponse.Headers
                .GetValues("Set-Cookie")
                .First(c => c.StartsWith("auth_token"));
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            return client;
        }
    }
}
