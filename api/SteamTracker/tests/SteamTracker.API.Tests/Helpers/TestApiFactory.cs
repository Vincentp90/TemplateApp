using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.Messaging;
using SteamTracker.Application.Ports;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using RabbitMQ.Client;
using System.Text.Json;

namespace SteamTracker.API.Tests.Helpers;

/// <summary>
/// Test factory for SteamTracker API using testcontainers.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18.1")
        .WithPassword("testpassword")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.2-management")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove hosted services (workers) — we test their logic directly
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var svc in hostedServices)
                services.Remove(svc);

            // Replace DB
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SteamTrackerDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<SteamTrackerDbContext>(options =>
                options.UseNpgsql(_db.GetConnectionString()).UseSnakeCaseNamingConvention());

            // Replace RabbitMQ connection factory — the ChannelPool and ExchangeInitializer
            // resolve Func<Task<IConnection>> from DI, so replacing it here ensures the
            // test container connection is used instead of localhost.
            var testConnectionFactory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMq.GetConnectionString()),
            };
            services.AddSingleton<Func<Task<IConnection>>>(_ =>
                async () => await testConnectionFactory.CreateConnectionAsync());
        });
    }

    private HttpClient? _client;
    private bool _seeded;

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        await _rabbitMq.StartAsync();
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
    public override async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Seeds the database with test data. Must be called before CreateClient.
    /// </summary>
    public async Task SeedAsync(Func<IServiceProvider, Task> seed)
    {
        // Create a scope and run seed before the host is disposed
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<SteamTrackerDbContext>();
        await context.Database.MigrateAsync();
        await seed(sp);
    }

    /// <summary>
    /// Creates an HttpClient, seeding default data on first call.
    /// </summary>
    public HttpClient GetOrCreateClient()
    {
        if (_client == null)
        {
            _client = CreateClient();
            if (!_seeded)
            {
                SeedDefaultDataAsync().GetAwaiter().GetResult();
                _seeded = true;
            }
        }
        return _client;
    }

    private async Task SeedDefaultDataAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<SteamTrackerDbContext>();
        await context.Database.MigrateAsync();

        var trackedGameRepo = sp.GetRequiredService<ITrackedGameRepository>();
        var gameRepo = sp.GetRequiredService<IGameRepository>();

        var trackedGame = TrackedGame.StartTracking(new SteamAppId(42), DateTimeOffset.UtcNow);
        await trackedGameRepo.SaveAsync(trackedGame);

        var game = new Game(new SteamAppId(42));
        game.ApplyPriceUpdate(new Money(19.99m, "EUR"), "Test Game", DateTimeOffset.UtcNow);
        await gameRepo.SaveAsync(game);
    }
}

/// <summary>
/// In-memory stub for PriceCheckJobPublisher — captures enqueued appIds.
/// </summary>
public class InMemoryPriceCheckJobPublisher : IPriceCheckJobPublisher
{
    public List<int> EnqueuedAppIds { get; } = new();
    public Task EnqueueAsync(int appId, CancellationToken cancellationToken = default)
    {
        EnqueuedAppIds.Add(appId);
        return Task.CompletedTask;
    }
}
