using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using SteamTracker.Application.Ports;

namespace SteamTracker.Infrastructure.Messaging;

public class PriceCheckJobPublisher : IPriceCheckJobPublisher
{
    private readonly IConnection _connection;
    private readonly string _exchangeName;

    public PriceCheckJobPublisher(IConnection connection, IConfiguration configuration)
    {
        _connection = connection;
        _exchangeName = configuration["RabbitMQ:PriceCheckExchange"] ?? "steamtracker.pricecheck";
    }

    public async Task EnqueueAsync(int appId, CancellationToken cancellationToken = default)
    {
        await using var channel = await _connection.CreateChannelAsync(null, cancellationToken);
        await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue: "pricecheck.jobs", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue: "pricecheck.jobs", exchange: _exchangeName, routingKey: "pricecheck", arguments: null, cancellationToken: cancellationToken);

        var body = JsonSerializer.Serialize(new { AppId = appId, EnqueuedAt = DateTimeOffset.UtcNow });
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: "pricecheck",
            mandatory: false,
            body: bodyBytes,
            cancellationToken: cancellationToken);
    }
}
