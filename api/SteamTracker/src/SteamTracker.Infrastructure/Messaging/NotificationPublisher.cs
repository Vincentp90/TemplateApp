using System.Text;
using System.Text.Json;
using CrossService.Messaging;
using SteamTracker.Application.Ports;

namespace SteamTracker.Infrastructure.Messaging;

/// <summary>
/// Publishes alert notifications to a RabbitMQ topic exchange using a shared channel pool.
/// Messages are published as persistent with content-type and message-id metadata.
/// Exchange declaration is handled by ExchangeInitializer at startup.
/// </summary>
public class NotificationPublisher : INotificationPublisher
{
    private readonly ChannelPool _channelPool;
    private readonly string _exchangeName;
    private readonly JsonSerializerOptions _jsonOptions;

    public NotificationPublisher(
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

    public async Task PublishAsync(Guid alertRuleId, string userId, int appId, decimal price, string currency, CancellationToken cancellationToken = default)
    {
        var channel = await _channelPool.GetChannelAsync(cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                AlertRuleId = alertRuleId,
                UserId = userId,
                AppId = appId,
                Price = price,
                Currency = currency,
                TriggeredAt = DateTimeOffset.UtcNow
            },
            _jsonOptions);

        var props = new RabbitMQ.Client.BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "alert.triggered",
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }
}
