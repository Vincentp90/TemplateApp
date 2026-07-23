using Application.Contracts;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.AppListings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RabbitMQ.Client;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Repositories;
using SteamTracker.Application.Ports;
using Application;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WishlistApi;
using WishlistApi.Controllers;

namespace CrossService.Tests;

/// <summary>
/// Shared setup for cross-service integration tests.
/// Manages Docker containers, the WebApplicationFactory, and seeding helpers.
/// </summary>
public class CrossServiceFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _wishlistDb;
    private PostgreSqlContainer? _steamDb;
    private RabbitMqContainer? _rabbitMq;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public SteamTrackerDbContext SteamContext { get; private set; } = null!;
    public HttpClient WishlistClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Create fresh containers for each test run (singleton fixture may be reused across tests)
        _wishlistDb = new PostgreSqlBuilder("postgres:18.1")
            .WithDatabase("wishlisttest")
            .WithUsername("user")
            .WithPassword("pass")
            .Build();

        _steamDb = new PostgreSqlBuilder("postgres:18.1")
            .WithDatabase("steamtest")
            .WithPassword("testpassword")
            .Build();

        _rabbitMq = new RabbitMqBuilder("rabbitmq:4.2-management")
            .Build();

        // Start containers
        await _wishlistDb.StartAsync();
        await _steamDb.StartAsync();
        await _rabbitMq.StartAsync();

        // Create SteamTracker DbContext and seed data
        var steamOptions = new DbContextOptionsBuilder<SteamTrackerDbContext>()
            .UseNpgsql(_steamDb!.GetConnectionString(), o => o.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .Options;
        SteamContext = new SteamTrackerDbContext(steamOptions);
        await SteamContext.Database.MigrateAsync();

        // Build WishlistApi factory with test containers
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove hosted services
                var hostedServices = services
                    .Where(d => d.ServiceType == typeof(IHostedService))
                    .ToList();
                foreach (var svc in hostedServices)
                    services.Remove(svc);

                // Replace WishlistApi DB
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<WishlistDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);
                services.AddDbContext<WishlistDbContext>(options =>
                    options.UseNpgsql(_wishlistDb!.GetConnectionString()).UseSnakeCaseNamingConvention());

                // Replace RabbitMQ with no-op
                var rmqFactoryDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IRabbitMqConnectionFactory));
                if (rmqFactoryDescriptor != null)
                    services.Remove(rmqFactoryDescriptor);
                services.AddSingleton<IRabbitMqConnectionFactory>(
                    _ => new NoOpRabbitMqConnectionFactory());
                services.AddScoped<IEventPublisher>(
                    _ => new NoOpRabbitMqEventPublisher());

                // Mock the SteamTracker HTTP client so PricesController returns seeded data
                services.AddHttpClient("SteamTracker")
                    .ConfigurePrimaryHttpMessageHandler(_ => new MockSteamTrackerHandler(SteamContext));
            });
        });

        // Create authenticated client
        var client = Factory.CreateClient();

        // Register a user
        var username = $"testuser-{Guid.NewGuid():N}";
        var password = Guid.NewGuid().ToString();
        await client.PostAsJsonAsync("/auth/register", new { Username = username, Password = password });

        // Login
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { Username = username, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        // Set auth cookie (dev/test environment has no SSL)
        var cookie = loginResponse.Headers
            .GetValues("Set-Cookie")
            .First(c => c.StartsWith("auth_token"));
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        WishlistClient = client;
    }

    public async Task DisposeAsync()
    {
        if (_wishlistDb != null) await _wishlistDb.DisposeAsync();
        if (_steamDb != null) await _steamDb.DisposeAsync();
        if (_rabbitMq != null) await _rabbitMq.DisposeAsync();
        SteamContext?.Dispose();
        Factory?.Dispose();
        WishlistClient?.Dispose();
    }

    /// <summary>
    /// Seeds an app listing in the WishlistApi database.
    /// </summary>
    public async Task SeedWishlistAppListingAsync(int appId, string name)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
        dbContext.AppListings.Add(new AppListing { appid = appId, name = name });
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a tracked game and its price data directly into SteamTracker's database.
    /// </summary>
    public async Task SeedSteamTrackerGameAsync(int appId, string gameName, decimal price)
    {
        // Seed tracked game
        var trackedGameRepo = new TrackedGameRepository(SteamContext);
        var trackedGame = TrackedGame.StartTracking(new SteamAppId(appId), DateTimeOffset.UtcNow);
        await trackedGameRepo.SaveAsync(trackedGame);

        // Seed game with price
        var gameRepo = new GameRepository(SteamContext);
        var game = new Game(new SteamAppId(appId));
        game.ApplyPriceUpdate(new Money(price, "EUR"), gameName, DateTimeOffset.UtcNow);
        await gameRepo.SaveAsync(game);
    }
}

/// <summary>
/// No-op RabbitMQ connection factory — used in integration tests so no real broker is needed.
/// </summary>
public class NoOpRabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    public Task<IConnection> CreateConnectionAsync()
    {
        throw new InvalidOperationException("No-op factory — no real connection available");
    }
}

/// <summary>
/// No-op event publisher — silently discards all events.
/// </summary>
public class NoOpRabbitMqEventPublisher : IEventPublisher
{
    public Task PublishAsync(object @event)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock SteamTracker HTTP handler that reads game prices from the SteamTracker DbContext.
/// </summary>
public class MockSteamTrackerHandler : DelegatingHandler
{
    private readonly SteamTrackerDbContext _context;

    public MockSteamTrackerHandler(SteamTrackerDbContext context)
    {
        _context = context;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Parse the appIds query parameter
        var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
        var appIdStrings = query.GetValues("appIds") ?? Array.Empty<string>();
        var appIds = new HashSet<int>(appIdStrings.Select(int.Parse));

        // Query games from the database (fetch all, filter client-side since AppId is a value object)
        var allGames = await _context.Games.ToListAsync(cancellationToken);
        var games = allGames.Where(g => appIds.Contains(g.AppId.Value)).ToList();

        var prices = games.Select(g =>
        {
            var amount = g.CurrentPrice?.Amount;
            var currency = g.CurrentPrice?.Currency;
            return new GamePriceDto(
                g.AppId.Value,
                amount ?? 0m,
                currency?.Value ?? "USD",
                g.LastCheckedAt,
                g.IsUnavailable
            );
        }).ToList();

        var json = JsonSerializer.Serialize(prices);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
