using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SteamTracker.Application.Ports;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Worker;

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
