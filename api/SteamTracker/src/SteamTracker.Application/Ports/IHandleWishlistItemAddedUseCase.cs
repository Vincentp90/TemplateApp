using System.Threading;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Called by WishlistSyncWorker when a WishlistItemAdded event arrives from the existing app.
/// Upserts a TrackedGame record and triggers the first price-check job.
/// </summary>
public interface IHandleWishlistItemAddedUseCase
{
    Task ExecuteAsync(string userId, int appId, DateTimeOffset addedAt, CancellationToken cancellationToken = default);
}
