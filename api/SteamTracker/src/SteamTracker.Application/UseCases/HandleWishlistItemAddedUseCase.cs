using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Called by WishlistSyncWorker when a WishlistItemAdded event arrives.
/// Upserts a TrackedGame and triggers the first price-check job.
/// </summary>
public class HandleWishlistItemAddedUseCase : IHandleWishlistItemAddedUseCase
{
    private readonly ITrackedGameRepository _trackedGameRepo;
    private readonly IPriceCheckJobPublisher _priceCheckJobpublisher;

    public HandleWishlistItemAddedUseCase(
        ITrackedGameRepository trackedGameRepo,
        IPriceCheckJobPublisher publisher)
    {
        _trackedGameRepo = trackedGameRepo;
        _priceCheckJobpublisher = publisher;
    }

    public async Task ExecuteAsync(string userId, int appId, DateTimeOffset addedAt, CancellationToken cancellationToken = default)
    {
        var existing = await _trackedGameRepo.GetAsync(appId, cancellationToken);
        if (existing is not null && existing.IsActive)
            return; // Already tracking — idempotent no-op

        var trackedGame = TrackedGame.StartTracking(appId, addedAt);
        await _trackedGameRepo.SaveAsync(trackedGame, cancellationToken);

        await _priceCheckJobpublisher.EnqueueAsync(appId, cancellationToken);
    }
}
