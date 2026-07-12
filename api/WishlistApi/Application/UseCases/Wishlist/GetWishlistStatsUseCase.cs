using Application.UseCases;
using Application.UseCases.Wishlist.Requests;
using Domain.Repositories;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: retrieve statistics for a user's wishlist.
/// </summary>
public class GetWishlistStatsUseCase(IWishlistItemRepository wishlistItemRepository) : IGetWishlistStatsUseCase
{
    public async Task<Domain.WishlistStats> ExecuteAsync(GetWishlistStatsRequest request)
    {
        var items = await wishlistItemRepository.GetWishlistItemsAsync(request.UserId);
        return Domain.WishlistItem.CalculateStats(items);
    }
}
