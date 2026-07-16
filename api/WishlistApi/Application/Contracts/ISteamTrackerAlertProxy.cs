namespace Application.Contracts;

/// <summary>
/// Proxy for SteamTracker's alert management endpoints.
/// Used by WishlistApi to delegate alert creation/deletion to SteamTracker's business logic.
/// </summary>
public interface ISteamTrackerAlertProxy
{
    Task SetAlertRuleAsync(string userId, int appId, decimal thresholdAmount, string currency = "EUR");
    Task DeleteAlertRuleAsync(string userId, Guid alertRuleId);
    Task<List<AlertRuleInfo>> GetAlertRulesAsync(string userId);
}

/// <summary>
/// DTO for alert rule info returned from SteamTracker proxy.
/// </summary>
public record AlertRuleInfo(Guid AlertRuleId, int AppId, decimal ThresholdAmount, string Currency);
