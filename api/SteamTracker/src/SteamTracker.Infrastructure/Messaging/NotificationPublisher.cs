using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using SteamTracker.Application.Ports;

namespace SteamTracker.Infrastructure.Messaging;

public class NotificationPublisher : INotificationPublisher
{
    private readonly IConnection _connection;
    private readonly string _exchangeName;

    public NotificationPublisher(IConnection connection, IConfiguration configuration)
    {
        _connection = connection;
        _exchangeName = configuration["RabbitMQ:NotificationExchange"] ?? "steamtracker.notifications";
    }

    public async Task PublishAsync(Guid alertRuleId, string userId, int appId, decimal price, string currency, CancellationToken cancellationToken = default)
    {
        await using var channel = await _connection.CreateChannelAsync(null, cancellationToken);
        await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = JsonSerializer.Serialize(
            new
            {
                AlertRuleId = alertRuleId,
                UserId = userId,
                AppId = appId,
                Price = price,
                Currency = currency,
                TriggeredAt = DateTimeOffset.UtcNow
            },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        var properties = new BasicProperties
        {
            Persistent = true
        };

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "alert.triggered",
            mandatory: false,
            basicProperties: properties,
            body: bodyBytes,
            cancellationToken: cancellationToken);
    }
}
