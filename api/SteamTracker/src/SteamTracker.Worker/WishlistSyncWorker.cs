using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;

namespace SteamTracker.Worker;

/// <summary>
/// Consumes wishlist events from the ACL exchange and dispatches to use cases.
/// </summary>
public class WishlistSyncWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IHandleWishlistItemAddedUseCase _addedUseCase;
    private readonly IHandleWishlistItemRemovedUseCase _removedUseCase;
    private readonly ILogger<WishlistSyncWorker> _logger;
    private const string ExchangeName = "wishlist.events";
    private const string QueueName = "steamtracker.wishlist-sync";

    public WishlistSyncWorker(
        IConnection connection,
        IHandleWishlistItemAddedUseCase addedUseCase,
        IHandleWishlistItemRemovedUseCase removedUseCase,
        ILogger<WishlistSyncWorker> logger)
    {
        _connection = connection;
        _addedUseCase = addedUseCase;
        _removedUseCase = removedUseCase;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(null, stoppingToken);

        // Declare the fanout exchange (idempotent — existing app also declares it)
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);

        // Declare and bind our queue
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, routingKey: "", cancellationToken: stoppingToken);

        var consumer = new WishlistSyncConsumer(_addedUseCase, _removedUseCase, channel, _logger);
        await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
    }
}
