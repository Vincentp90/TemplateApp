using Application;
using Infrastructure.Messaging;
using RabbitMQ.Client;

namespace Tests.Helpers;

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
/// Used in integration tests so the app doesn't need a real RabbitMQ broker.
/// </summary>
public class NoOpRabbitMqEventPublisher : IEventPublisher
{
    public Task PublishAsync(object @event)
    {
        // Silently discard — events are not published in test mode
        return Task.CompletedTask;
    }
}
