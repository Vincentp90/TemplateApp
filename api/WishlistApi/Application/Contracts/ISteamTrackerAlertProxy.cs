namespace Application.Contracts;

/// <summary>
/// Proxy for SteamTracker's alert management endpoints.
/// Used by WishlistApi to delegate alert creation/deletion to SteamTracker's business logic.
/// </summary>
public interface ISteamTrackerAlertProxy
{
    Task SetAlertRuleAsync(string userId, int appId, decimal thresholdAmount, string currency = "EUR");
    Task DeleteAlertRuleAsync(string userId, Guid alertRuleId);
}
