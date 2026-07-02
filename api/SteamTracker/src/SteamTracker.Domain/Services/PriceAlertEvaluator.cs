using SteamTracker.Domain.Entities;

namespace SteamTracker.Domain.Services;

/// <summary>
/// Pure domain service — evaluates alert rules against a game's current price.
/// No I/O, no framework dependencies.
/// </summary>
public class PriceAlertEvaluator
{
    public IEnumerable<AlertRule> Evaluate(Game game, IEnumerable<AlertRule> rules)
    {
        if (game.CurrentPrice is null || rules is null)
            return [];

        var price = game.CurrentPrice.Value;
        return rules
            .Where(r => r.IsActive)
            .Where(r => r.ShouldTrigger(price))
            .ToList();
    }
}
