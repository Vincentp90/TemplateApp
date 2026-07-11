using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Called by WishlistSyncWorker when a WishlistItemAdded event arrives.
/// Upserts a TrackedGame and triggers the first price-check job if the game
/// is due for a price check (never checked, or last check was >24h ago).
/// </summary>
public class HandleWishlistItemAddedUseCase : IHandleWishlistItemAddedUseCase
{
    private readonly ITrackedGameRepository _trackedGameRepo;
    private readonly IGameRepository _gameRepo;
    private readonly IPriceCheckJobPublisher _priceCheckJobPublisher;

    public HandleWishlistItemAddedUseCase(
        ITrackedGameRepository trackedGameRepo,
        IGameRepository gameRepo,
        IPriceCheckJobPublisher publisher)
    {
        _trackedGameRepo = trackedGameRepo;
        _gameRepo = gameRepo;
        _priceCheckJobPublisher = publisher;
    }

    public async Task ExecuteAsync(string userId, int appId, DateTimeOffset addedAt, CancellationToken cancellationToken = default)
    {
        var existing = await _trackedGameRepo.GetAsync(new SteamAppId(appId), cancellationToken);
        if (existing is not null && existing.IsActive)
        {
            // Already tracking — only enqueue if the game is due for a price check
            var game = await _gameRepo.GetAsync(new SteamAppId(appId), cancellationToken);
            var isDue = game is null || game.CanPriceCheck(addedAt);
            if (isDue)
                await _priceCheckJobPublisher.EnqueueAsync(appId, cancellationToken);
            return;
        }

        var trackedGame = TrackedGame.StartTracking(new SteamAppId(appId), addedAt);
        await _trackedGameRepo.SaveAsync(trackedGame, cancellationToken);

        // New games are always price-checked immediately
        await _priceCheckJobPublisher.EnqueueAsync(appId, cancellationToken);
    }
}
