using System.Text;
using System.Text.Json;
using Application;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to a RabbitMQ fanout exchange.
/// Each call creates a new async channel, declares the exchange, and publishes the serialized event.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMqEventPublisher(
        IRabbitMqConnectionFactory connectionFactory,
        string exchangeName,
        JsonSerializerOptions? jsonOptions = null)
    {
        _connectionFactory = connectionFactory;
        _exchangeName = exchangeName;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public async Task PublishAsync(object @event)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Fanout, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, _jsonOptions));

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "",
            mandatory: false,
            body: body);
    }
}
