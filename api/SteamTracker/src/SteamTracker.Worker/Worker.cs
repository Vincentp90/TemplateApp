using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.External;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Worker;

/// <summary>
/// Consumes price-check jobs from RabbitMQ, fetches prices from Steam, and processes them.
/// </summary>
public class PriceCheckWorker : BackgroundService
{
    private readonly IProcessPriceCheckUseCase _useCase;
    private readonly ISteamStoreClient _steamClient;
    private readonly IConnection _connection;
    private readonly string _queueName;
    private readonly ILogger<PriceCheckWorker> _logger;

    public PriceCheckWorker(
        IProcessPriceCheckUseCase useCase,
        ISteamStoreClient steamClient,
        IConnection connection,
        IConfiguration configuration,
        ILogger<PriceCheckWorker> logger)
    {
        _useCase = useCase;
        _steamClient = steamClient;
        _connection = connection;
        _queueName = configuration["RabbitMQ:PriceCheckQueue"] ?? "pricecheck.jobs";
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(null, stoppingToken);
        await channel.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var consumer = new PriceCheckConsumer(_useCase, _steamClient, channel, _logger);
        await channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
    }
}

/// <summary>
/// Custom async consumer for price-check jobs.
/// </summary>
public class PriceCheckConsumer : AsyncEventingBasicConsumer
{
    private readonly IProcessPriceCheckUseCase _useCase;
    private readonly ISteamStoreClient _steamClient;
    private readonly IChannel _channel;
    private readonly ILogger _logger;

    public PriceCheckConsumer(
        IProcessPriceCheckUseCase useCase,
        ISteamStoreClient steamClient,
        IChannel channel,
        ILogger logger)
        : base(channel)
    {
        _useCase = useCase;
        _steamClient = steamClient;
        _channel = channel;
        _logger = logger;
    }

    public override async Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(body.ToArray());
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var request = JsonSerializer.Deserialize<PriceCheckMessage>(json, options);

            if (request is null)
            {
                _logger.LogWarning("Received null PriceCheckMessage");
                await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken);
                return;
            }

            // Fetch price from Steam
            var result = await _steamClient.FetchPriceAsync(request.AppId);

            if (result is null)
            {
                _logger.LogWarning("Steam returned no price data for AppId {AppId}, requeuing", request.AppId);
                await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken);
                return;
            }

            var (price, name) = result.Value;
            await _useCase.ExecuteAsync(request.AppId, price, name, cancellationToken);
            await _channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken);
        }
        catch (SteamRateLimitException ex)
        {
            _logger.LogWarning(ex, "Steam rate limit hit for delivery {DeliveryTag}, requeuing", deliveryTag);
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken);
        }
        catch (Exception ex)
        {
            var requeue = WorkerHelpers.IsTransient(ex);
            _logger.LogError(ex, "Error processing price check for delivery {DeliveryTag} (requeue: {Requeue})", deliveryTag, requeue);
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue, cancellationToken);
        }
    }
}

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

/// <summary>
/// Single async consumer for both wishlist event types from the ACL exchange.
/// Dispatches to the appropriate use case based on event type.
/// </summary>
public class WishlistSyncConsumer : AsyncEventingBasicConsumer
{
    private readonly IHandleWishlistItemAddedUseCase _addedUseCase;
    private readonly IHandleWishlistItemRemovedUseCase _removedUseCase;
    private readonly IChannel _channel;
    private readonly ILogger _logger;

    public WishlistSyncConsumer(
        IHandleWishlistItemAddedUseCase addedUseCase,
        IHandleWishlistItemRemovedUseCase removedUseCase,
        IChannel channel,
        ILogger logger)
        : base(channel)
    {
        _addedUseCase = addedUseCase;
        _removedUseCase = removedUseCase;
        _channel = channel;
        _logger = logger;
    }

    public override async Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(body.ToArray());
            var doc = JsonDocument.Parse(json);

            // Determine event type by checking for the "removed_at" field
            // WishlistItemAdded has: user_id, app_id, added_at
            // WishlistItemRemoved has: user_id, app_id, removed_at
            var hasRemovedAt = doc.RootElement.TryGetProperty("removed_at", out _);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            if (hasRemovedAt)
            {
                var evt = JsonSerializer.Deserialize<WishlistItemRemovedMessage>(json, options);
                if (evt is not null)
                    await _removedUseCase.ExecuteAsync(evt.UserId, evt.AppId, cancellationToken);
            }
            else
            {
                var evt = JsonSerializer.Deserialize<WishlistItemAddedMessage>(json, options);
                if (evt is not null)
                    await _addedUseCase.ExecuteAsync(evt.UserId, evt.AppId, evt.AddedAt, cancellationToken);
            }

            await _channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception ex)
        {
            var requeue = WorkerHelpers.IsTransient(ex);
            _logger.LogError(ex, "Error processing wishlist sync message (requeue: {Requeue})", requeue);
            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: requeue, cancellationToken);
        }
    }
}

internal static class WorkerHelpers
{
    /// <summary>
    /// Classifies an exception as transient (retryable) or programming error (dead-letter).
    /// </summary>
    public static bool IsTransient(Exception ex)
    {
        return ex is
            TimeoutException or
            OperationCanceledException or
            HttpRequestException or
            SteamRateLimitException or
            IOException;
    }
}

// Shared DTOs
public record PriceCheckMessage(int AppId, DateTimeOffset EnqueuedAt);

// ACL message contracts — match the wire format from the existing app
public record WishlistItemAddedMessage(string UserId, int AppId, DateTimeOffset AddedAt);
public record WishlistItemRemovedMessage(string UserId, int AppId, DateTimeOffset RemovedAt);


