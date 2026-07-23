using System.Threading.RateLimiting;
using CrossService.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Infrastructure.External;
using SteamTracker.Infrastructure.Messaging;
using SteamTracker.Infrastructure.Repositories;

namespace SteamTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core DbContext
        var connectionString = configuration.GetConnectionString("SteamTracker")
            ?? throw new InvalidOperationException("Connection string 'SteamTracker' not configured.");

        services.AddDbContext<SteamTrackerDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        // Repositories
        services.AddScoped<ITrackedGameRepository, TrackedGameRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();

        // External clients
        services.AddSingleton<TokenBucketRateLimiter>(_ => new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {   // API supposedly has 200 req/5 min limit
                QueueLimit = 1,
                TokenLimit = 50,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(3),// 100 req/5 min
                AutoReplenishment = true
            }));
        services.AddHttpClient<ISteamStoreClient, SteamStoreClient>();

        // RabbitMQ connection — registered as a factory so connection creation is deferred
        // until first use. This allows test factories to replace the connection before
        // the ChannelPool is first accessed.
        var rabbitSettings = configuration.GetSection("RabbitMQ").Get<RabbitMqSettings>()
            ?? new RabbitMqSettings();

        var rmqFactory = new ConnectionFactory()
        {
            HostName = rabbitSettings.HostName ?? "localhost",
            Port = rabbitSettings.Port ?? 5672,
            UserName = rabbitSettings.UserName ?? "guest",
            Password = rabbitSettings.Password ?? "guest",
            VirtualHost = rabbitSettings.VirtualHost ?? "/",
            AutomaticRecoveryEnabled = true
        };

        // Register a connection factory delegate — test factories can replace this.
        // Wrapped in async lambda to convert ValueTask<IConnection> to Task<IConnection>.
        services.AddSingleton<Func<Task<IConnection>>>(sp => async () => await rmqFactory.CreateConnectionAsync());

        // Channel pool with lazy connection creation — resolves Func<Task<IConnection>> from DI
        services.AddSingleton<ChannelPool>(sp =>
            new ChannelPool(sp.GetRequiredService<Func<Task<IConnection>>>()));

        // Exchange initializer shares the same ChannelPool so exchanges are declared on the
        // active connection, not a separate one.
        services.AddSingleton<ExchangeInitializer>(sp =>
            new ExchangeInitializer(sp.GetRequiredService<ChannelPool>()));



        // Publishers — use the shared channel pool
        services.AddScoped<INotificationPublisher>(sp =>
            new NotificationPublisher(
                sp.GetRequiredService<ChannelPool>(),
                "steamtracker.notifications"));
        services.AddScoped<IPriceCheckJobPublisher>(sp =>
            new PriceCheckJobPublisher(
                sp.GetRequiredService<ChannelPool>(),
                "steamtracker.pricecheck",
                "pricecheck.jobs",
                "pricecheck"));

        return services;
    }
}

public class RabbitMqSettings
{
    public string? HostName { get; init; }
    public int? Port { get; init; }
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? VirtualHost { get; init; }
}
