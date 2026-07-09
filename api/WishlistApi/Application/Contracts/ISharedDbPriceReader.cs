namespace Application.Contracts;

/// <summary>
/// Reads price data and alert rules from SteamTracker's database (shared DB).
/// Uses raw SQL via Dapper to avoid EF Core naming conflicts.
/// </summary>
public interface ISharedDbPriceReader
{
    /// <summary>
    /// Reads current prices for the given app IDs from the games table.
    /// Returns a dictionary keyed by AppId.
    /// </summary>
    Task<Dictionary<int, GamePrice>> GetPricesAsync(IEnumerable<int> appIds);

    /// <summary>
    /// Reads active alert rules for the given user from the alert_rules table.
    /// Returns a dictionary of AppId → AlertRuleInfo.
    /// </summary>
    Task<Dictionary<int, AlertRuleInfo>> GetAlertRulesAsync(string userId);
}

public record GamePrice(decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable = false);
public record AlertRuleInfo(Guid Id, decimal ThresholdAmount, string Currency);
