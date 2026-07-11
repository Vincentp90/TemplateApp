using Application.Contracts;
using Application.UseCases.Wishlist.Requests;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: delete an alert rule (delegates to SteamTracker).
/// </summary>
public class DeleteAlertRuleUseCase(ISteamTrackerAlertProxy alertProxy) : IDeleteAlertRuleUseCase
{
    public async Task ExecuteAsync(DeleteAlertRuleRequest request)
    {
        await alertProxy.DeleteAlertRuleAsync(request.UserId, request.AlertRuleId);
    }
}
