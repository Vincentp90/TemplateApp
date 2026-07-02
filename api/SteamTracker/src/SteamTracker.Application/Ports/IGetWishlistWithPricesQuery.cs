using System.Threading;
using SteamTracker.Application.DTOs;

namespace SteamTracker.Application.Ports;

/// <summary>
/// Returns the user's wishlist with current prices, joined from local TrackedGame + Game data.
/// </summary>
public interface IGetWishlistWithPricesQuery
{
    Task<IReadOnlyList<WishlistItemWithPriceDto>> ExecuteAsync(string userId, CancellationToken cancellationToken = default);
}
