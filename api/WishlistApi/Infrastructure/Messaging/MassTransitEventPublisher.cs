using Application;
using MassTransit;

namespace Infrastructure.Messaging;

/// <summary>
/// Publishes domain events through MassTransit's IBus.
/// Replaces the raw RabbitMQ.Client-based publisher with a cleaner,
/// connection-pooled, exchange-declaring MassTransit implementation.
/// </summary>
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IBus _bus;

    public MassTransitEventPublisher(IBus bus)
    {
        _bus = bus;
    }

    public Task PublishAsync(object @event)
    {
        return _bus.Publish(@event);
    }
}
