namespace SteamTracker.API.Models;

/// <summary>
/// DTO for returning alert rule information from SteamTracker.
/// </summary>
public record AlertRuleDto(Guid AlertRuleId, int AppId, decimal ThresholdAmount, string Currency);
