using Application.Contracts;
using Application.UseCases.Wishlist.Requests;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: set an alert rule for a wishlisted game (delegates to SteamTracker).
/// </summary>
public class SetAlertRuleUseCase(ISteamTrackerAlertProxy alertProxy) : ISetAlertRuleUseCase
{
    public async Task ExecuteAsync(SetAlertRuleRequest request)
    {
        await alertProxy.SetAlertRuleAsync(request.UserId, request.AppId, request.ThresholdAmount, request.Currency);
    }
}
