using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Worker;

/// <summary>
/// Runs every 24 hours and enqueues a price-check job for every active TrackedGame.
/// This is the "scheduler" that drives the periodic price fetching pipeline.
/// </summary>
public class PriceCheckScheduler : BackgroundService
{
    private readonly ITrackedGameRepository _trackedGameRepo;
    private readonly IPriceCheckJobPublisher _publisher;
    private readonly ILogger<PriceCheckScheduler> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public PriceCheckScheduler(
        ITrackedGameRepository trackedGameRepo,
        IPriceCheckJobPublisher publisher,
        ILogger<PriceCheckScheduler> logger)
    {
        _trackedGameRepo = trackedGameRepo;
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
        var activeGames = await _trackedGameRepo.GetActiveAsync(cancellationToken);
        var uniqueAppIds = activeGames
            .Select(tg => tg.AppId.Value)
            .Distinct()
            .ToList();

        foreach (var appId in uniqueAppIds)
        {
            try
            {
                await _publisher.EnqueueAsync(appId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue price-check job for AppId {AppId}", appId);
            }
        }
    }
}
