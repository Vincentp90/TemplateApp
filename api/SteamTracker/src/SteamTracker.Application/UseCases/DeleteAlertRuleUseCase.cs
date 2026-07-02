using SteamTracker.Application.Ports;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Deletes an existing alert rule.
/// </summary>
public class DeleteAlertRuleUseCase : IDeleteAlertRuleUseCase
{
    private readonly IAlertRuleRepository _alertRuleRepo;

    public DeleteAlertRuleUseCase(IAlertRuleRepository alertRuleRepo)
    {
        _alertRuleRepo = alertRuleRepo;
    }

    public async Task ExecuteAsync(string userId, Guid alertRuleId, CancellationToken cancellationToken = default)
    {
        var rule = await _alertRuleRepo.GetAsync(alertRuleId, cancellationToken);
        if (rule is null || rule.UserId != userId)
            throw new InvalidOperationException($"Alert rule {alertRuleId} not found for user {userId}.");

        await _alertRuleRepo.DeleteAsync(rule, cancellationToken);
    }
}
