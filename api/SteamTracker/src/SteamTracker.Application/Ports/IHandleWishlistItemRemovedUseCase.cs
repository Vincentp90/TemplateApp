using System.Threading;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Called by WishlistSyncWorker when a WishlistItemRemoved event arrives from the existing app.
/// Deactivates the TrackedGame and all its AlertRules.
/// </summary>
public interface IHandleWishlistItemRemovedUseCase
{
    Task ExecuteAsync(string userId, int appId, CancellationToken cancellationToken = default);
}
