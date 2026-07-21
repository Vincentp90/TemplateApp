using System.Text;
using System.Text.Json;
using Application;
using CrossService.Messaging;

namespace Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to a RabbitMQ fanout exchange using a shared channel pool.
/// Messages are published as persistent with content-type and message-id metadata.
/// Exchange declaration is handled by ExchangeInitializer at startup — not per-publish.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly ChannelPool _channelPool;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMqEventPublisher(
        ChannelPool channelPool,
        string exchangeName,
        JsonSerializerOptions? jsonOptions = null)
    {
        _channelPool = channelPool ?? throw new ArgumentNullException(nameof(channelPool));
        _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public async Task PublishAsync(object @event)
    {
        var channel = await _channelPool.GetChannelAsync();

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, _jsonOptions);

        var props = new RabbitMQ.Client.BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "",
            mandatory: false,
            basicProperties: props,
            body: body);
    }
}
