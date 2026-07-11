using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;
using SteamTracker.Infrastructure.External;
using System.Text;
using System.Text.Json;

namespace SteamTracker.Worker;

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
            var result = await _steamClient.FetchPriceAsync(request.AppId, cancellationToken);

            if (result is not null && (result.Price != null || result.IsUnavailable))
            {
                await _useCase.ExecuteAsync(request.AppId, result.Price, result.Name, result.IsUnavailable, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Steam returned no price data for AppId {AppId} — still acking to avoid infinite retries", request.AppId);
            }

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
