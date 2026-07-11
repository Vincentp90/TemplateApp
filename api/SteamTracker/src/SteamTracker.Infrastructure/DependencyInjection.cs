using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        services.AddSingleton<IConnection>(_ => factory.CreateConnectionAsync().GetAwaiter().GetResult());

        // Publishers
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<IPriceCheckJobPublisher, PriceCheckJobPublisher>();

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
