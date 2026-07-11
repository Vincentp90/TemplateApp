using SteamTracker.Application.Ports;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Called by WishlistSyncWorker when a WishlistItemRemoved event arrives.
/// Deactivates the TrackedGame and all its AlertRules.
/// </summary>
public class HandleWishlistItemRemovedUseCase : IHandleWishlistItemRemovedUseCase
{
    private readonly ITrackedGameRepository _trackedGameRepo;
    private readonly IAlertRuleRepository _alertRuleRepo;

    public HandleWishlistItemRemovedUseCase(
        ITrackedGameRepository trackedGameRepo,
        IAlertRuleRepository alertRuleRepo)
    {
        _trackedGameRepo = trackedGameRepo;
        _alertRuleRepo = alertRuleRepo;
    }

    public async Task ExecuteAsync(string userId, int appId, CancellationToken cancellationToken = default)
    {
        var trackedGame = await _trackedGameRepo.GetAsync(appId, cancellationToken);
        if (trackedGame is null || !trackedGame.IsActive)
            return;

        trackedGame.StopTracking();
        await _trackedGameRepo.SaveAsync(trackedGame, cancellationToken);

        var rules = await _alertRuleRepo.GetForUserAsync(userId, cancellationToken);
        foreach (var rule in rules.Where(r => r.AppId.Value == appId))
        {
            rule.Deactivate();
            await _alertRuleRepo.SaveAsync(rule, cancellationToken);
        }
    }
}
