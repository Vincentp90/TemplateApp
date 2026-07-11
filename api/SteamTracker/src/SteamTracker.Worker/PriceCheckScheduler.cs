using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Worker;

/// <summary>
/// Runs every 24 hours and enqueues a price-check job for every game that is due.
/// A game is due if it has never been price-checked, or if more than 24 hours have passed
/// since the last price check (based on Game.LastCheckedAt).
/// </summary>
public class PriceCheckScheduler : BackgroundService
{
    private readonly IGameRepository _gameRepo;
    private readonly IPriceCheckJobPublisher _publisher;
    private readonly ILogger<PriceCheckScheduler> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public PriceCheckScheduler(
        IGameRepository gameRepo,
        IPriceCheckJobPublisher publisher,
        ILogger<PriceCheckScheduler> logger)
    {
        _gameRepo = gameRepo;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceCheckScheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnqueueAllActiveGames(stoppingToken);
                _logger.LogInformation("PriceCheckScheduler completed a cycle. Next cycle in {Interval}", _checkInterval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PriceCheckScheduler error during cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("PriceCheckScheduler stopping");
    }

    private async Task EnqueueAllActiveGames(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var appIdsDue = await _gameRepo.GetAppIdsDueForPriceCheckAsync(now, cancellationToken);
        var uniqueAppIds = appIdsDue.Distinct().ToList();

        foreach (var appId in uniqueAppIds)
        {
            try
            {
                await _publisher.EnqueueAsync(appId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue price-check job for AppId {AppId}", appId.Value);
            }
        }
    }
}
