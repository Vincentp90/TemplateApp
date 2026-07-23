using CrossService.Messaging;
using Microsoft.Extensions.Hosting;

namespace WishlistApi.HostedServices;

/// <summary>
/// Hosted service that initializes RabbitMQ exchanges at application startup.
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
            new[] { new ExchangeDeclaration { ExchangeName = "wishlist.events", Type = "fanout", Durable = true } },
            Enumerable.Empty<QueueDeclaration>(),
            Enumerable.Empty<QueueBinding>(),
            cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
