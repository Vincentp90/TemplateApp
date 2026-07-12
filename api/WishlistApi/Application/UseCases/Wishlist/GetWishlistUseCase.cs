using Application.UseCases.Wishlist.Requests;
using Domain.Repositories;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: retrieve a user's wishlist items.
/// Returns domain entities only — controller handles price/alert merging.
/// </summary>
public class GetWishlistUseCase(IWishlistItemRepository wishlistItemRepository) : IGetWishlistUseCase
{
    public async Task<IReadOnlyList<Domain.WishlistItem>> ExecuteAsync(GetWishlistRequest request)
    {
        return await wishlistItemRepository.GetWishlistItemsAsync(request.UserId);
    }
}
