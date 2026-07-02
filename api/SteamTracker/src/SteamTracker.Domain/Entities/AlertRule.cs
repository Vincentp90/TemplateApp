using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Domain.Entities;

/// <summary>
/// Aggregate root — defines a price alert for a user on a specific game.
/// Only exists for (UserId, AppId) pairs that have an active TrackedGame.
/// </summary>
public class AlertRule
{
    public Guid AlertRuleId { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public SteamAppId AppId { get; private set; }
    public Money TriggerBelowPrice { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    private AlertRule() { } // EF Core constructor

    public AlertRule(Guid alertRuleId, string userId, SteamAppId appId, Money triggerBelowPrice)
    {
        AlertRuleId = alertRuleId;
        UserId = userId;
        AppId = appId;
        TriggerBelowPrice = triggerBelowPrice;
    }

    public bool ShouldTrigger(Money currentPrice)
    {
        if (currentPrice.IsFree) return false;
        return currentPrice <= TriggerBelowPrice;
    }

    public void MarkTriggered(DateTimeOffset at)
    {
        LastTriggeredAt = at;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
