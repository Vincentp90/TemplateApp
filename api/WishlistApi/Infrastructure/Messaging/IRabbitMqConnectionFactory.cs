using RabbitMQ.Client;

namespace Infrastructure.Messaging;

/// <summary>
/// Factory for creating RabbitMQ connections.
/// Implemented as a singleton so all publishers share the same connection pool.
/// </summary>
public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync();
}
