namespace Application.UseCases.Wishlist.Requests;

/// <summary>
/// Request for deleting an alert rule.
/// </summary>
public record DeleteAlertRuleRequest(string UserId, Guid AlertRuleId);
