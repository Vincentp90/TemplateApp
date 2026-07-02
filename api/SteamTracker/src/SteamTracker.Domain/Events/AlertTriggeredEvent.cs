using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Events;

/// <summary>
/// Raised when an alert rule is triggered by a price crossing the threshold.
/// </summary>
public record AlertTriggeredEvent(
    Guid AlertRuleId,
    string UserId,
    SteamAppId AppId,
    Money Price);
