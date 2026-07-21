using CrossService.Messaging;
using Microsoft.Extensions.Hosting;

namespace SteamTracker.API;

/// <summary>
/// Hosted service that initializes RabbitMQ exchanges and queues at application startup.
/// Runs after the DI container is fully built, so the shared ChannelPool already
/// has its factory registered and test factories can have replaced it.
/// </summary>
public class RabbitMqInitializationHostedService : IHostedService
{
    private readonly ExchangeInitializer _initializer;

    public RabbitMqInitializationHostedService(ExchangeInitializer initializer)
    {
        _initializer = initializer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _initializer.InitializeAsync(
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
            },
            cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
