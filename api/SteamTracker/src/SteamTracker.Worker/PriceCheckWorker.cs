using CrossService.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;
using SteamTracker.Infrastructure.External;

namespace SteamTracker.Worker;

/// <summary>
/// Consumes price-check jobs from RabbitMQ, fetches prices from Steam, and processes them.
/// </summary>
public class PriceCheckWorker : BackgroundService
{
    private readonly IProcessPriceCheckUseCase _useCase;
    private readonly ISteamStoreClient _steamClient;
    private readonly IConnection _connection;
    private readonly IConfiguration _configuration;
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
        _configuration = configuration;
        _queueName = configuration["RabbitMQ:PriceCheckQueue"] ?? "pricecheck.jobs";
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(null, stoppingToken);

        var consumer = new PriceCheckConsumer(_useCase, _steamClient, channel, _logger);
        await channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
    }
}
