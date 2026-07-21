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

        // RabbitMQ connection
        var rabbitSettings = configuration.GetSection("RabbitMQ").Get<RabbitMqSettings>()
            ?? new RabbitMqSettings();

        var factory = new ConnectionFactory()
        {
            HostName = rabbitSettings.HostName ?? "localhost",
            Port = rabbitSettings.Port ?? 5672,
            UserName = rabbitSettings.UserName ?? "guest",
            Password = rabbitSettings.Password ?? "guest",
            VirtualHost = rabbitSettings.VirtualHost ?? "/",
            AutomaticRecoveryEnabled = true
        };

        var rmqConnection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        services.AddSingleton<IConnection>(rmqConnection);
        services.AddSingleton<ChannelPool>(new ChannelPool(rmqConnection));

        // Exchange initializer for one-shot setup
        services.AddSingleton<ExchangeInitializer>();

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

        // Initialize exchanges and queues at startup
        var exchangeInitializer = new ExchangeInitializer(
            new ChannelPool(rmqConnection));
        exchangeInitializer.InitializeAsync(
            new[]
            {
                new ExchangeDeclaration { ExchangeName = "steamtracker.notifications", Type = "topic", Durable = true },
                new ExchangeDeclaration { ExchangeName = "steamtracker.pricecheck", Type = "direct", Durable = true }
            },
            new[]
            {
                new QueueDeclaration { QueueName = "pricecheck.jobs", Durable = true, Exclusive = false, AutoDelete = false }
            },
            new[]
            {
                new QueueBinding { QueueName = "pricecheck.jobs", ExchangeName = "steamtracker.pricecheck", RoutingKey = "pricecheck" }
            }).GetAwaiter().GetResult();

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
