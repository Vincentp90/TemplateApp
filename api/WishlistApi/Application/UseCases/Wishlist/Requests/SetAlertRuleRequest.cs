namespace Application.UseCases.Wishlist.Requests;

/// <summary>
/// Request for setting an alert rule for a wishlisted game.
/// </summary>
public record SetAlertRuleRequest(string UserId, int AppId, decimal ThresholdAmount, string Currency = "EUR");
