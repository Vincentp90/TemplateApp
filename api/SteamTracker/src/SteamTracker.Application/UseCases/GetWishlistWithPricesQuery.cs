using SteamTracker.Application.DTOs;
using SteamTracker.Application.Ports;
using SteamTracker.Domain.Entities;
using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Application.UseCases;

/// <summary>
/// Returns the user's wishlist with current prices.
/// Joins TrackedGame + Game data per user.
/// </summary>
public class GetWishlistWithPricesQuery : IGetWishlistWithPricesQuery
{
    private readonly ITrackedGameRepository _trackedGameRepo;
    private readonly IGameRepository _gameRepo;

    public GetWishlistWithPricesQuery(
        ITrackedGameRepository trackedGameRepo,
        IGameRepository gameRepo)
    {
        _trackedGameRepo = trackedGameRepo;
        _gameRepo = gameRepo;
    }

    public async Task<IReadOnlyList<WishlistItemWithPriceDto>> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, the repository would filter by user.
        // For now, we fetch all active tracked games and join with game prices.
        var trackedGames = await _trackedGameRepo.GetActiveAsync(cancellationToken);
        var results = new List<WishlistItemWithPriceDto>();

        foreach (var tracked in trackedGames)
        {
            var game = await _gameRepo.GetAsync(tracked.AppId, cancellationToken);
            if (game is null) continue;

            results.Add(new WishlistItemWithPriceDto(
                game.AppId.Value,
                game.Name,
                game.CurrentPrice?.Amount,
                game.CurrentPrice?.Currency ?? "EUR",
                game.CurrentPrice?.IsFree ?? false,
                game.LastCheckedAt,
                tracked.TrackedSince));
        }

        return results;
    }
}
