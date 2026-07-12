using Application.Contracts;
using Infrastructure.Persistence;
using Infrastructure.SharedDb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Net.Http.Json;

namespace Tests.IntegrationTests;

/// <summary>
/// WebApplicationFactory that configures both WishlistApi DB and SteamTracker DB
/// using a shared Postgres container. Used for integration tests of SharedDbPriceReader
/// and proxy alert endpoints.
/// </summary>
public class SharedDbApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SharedDbFixture _fixture = SharedDbFixture.Instance;
    private bool _initialized;
    private bool _seeded;

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

            // Replace WishlistApi DB connection
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WishlistDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);
            services.AddDbContext<WishlistDbContext>(options =>
                options.UseNpgsql(_fixture.WishlistApiConnectionString)
                       .UseSnakeCaseNamingConvention());

            // Replace SharedDbPriceReader with one pointing to SteamTracker DB
            var priceReaderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ISharedDbPriceReader));
            if (priceReaderDescriptor != null)
                services.Remove(priceReaderDescriptor);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:SteamTrackerConnection", _fixture.SteamTrackerConnectionString }
                })
                .Build();
            services.AddScoped<ISharedDbPriceReader>(_ => new SharedDbPriceReader(config));

            // Mock the SteamTracker proxy — we test the Dapper reader with real DB,
            // but proxy is tested separately with HTTP handler mocking
            var proxyDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ISteamTrackerAlertProxy));
            if (proxyDescriptor != null)
                services.Remove(proxyDescriptor);
            var proxyMock = new Mock<ISteamTrackerAlertProxy>();
            services.AddScoped(_ => proxyMock.Object);
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (!_initialized)
        {
            await _fixture.InitializeAsync();
            _initialized = true;
        }
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }

    /// <summary>
    /// Seeds SteamTracker tables with test data. Must be called before CreateClient.
    /// </summary>
    public async Task SeedSteamTrackerAsync()
    {
        if (!_seeded)
        {
            await _fixture.SeedSteamTrackerAsync(_fixture.SteamTrackerConnectionString);
            _seeded = true;
        }
    }

    /// <summary>
    /// Creates an authenticated client after seeding SteamTracker data.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        await SeedSteamTrackerAsync();
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
