namespace Application;

/// <summary>
/// Publishes domain events to the message broker.
/// Consumers of this interface should not know about RabbitMQ — this is a port.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(object @event);
}
