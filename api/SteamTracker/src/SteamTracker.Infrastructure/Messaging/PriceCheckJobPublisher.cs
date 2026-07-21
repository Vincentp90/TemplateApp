using System.Text;
using System.Text.Json;
using CrossService.Messaging;
using SteamTracker.Application.Ports;

namespace SteamTracker.Infrastructure.Messaging;

/// <summary>
/// Publishes price-check jobs to a RabbitMQ direct exchange using a shared channel pool.
/// Messages are published as persistent.
/// Exchange/queue declaration is handled by ExchangeInitializer at startup.
/// </summary>
public class PriceCheckJobPublisher : IPriceCheckJobPublisher
{
    private readonly ChannelPool _channelPool;
    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public PriceCheckJobPublisher(
        ChannelPool channelPool,
        string exchangeName,
        string queueName,
        string routingKey,
        JsonSerializerOptions? jsonOptions = null)
    {
        _channelPool = channelPool ?? throw new ArgumentNullException(nameof(channelPool));
        _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public async Task EnqueueAsync(int appId, CancellationToken cancellationToken = default)
    {
        var channel = await _channelPool.GetChannelAsync(cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(
            new { AppId = appId, EnqueuedAt = DateTimeOffset.UtcNow },
            _jsonOptions);

        var props = new RabbitMQ.Client.BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: _routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }
}
