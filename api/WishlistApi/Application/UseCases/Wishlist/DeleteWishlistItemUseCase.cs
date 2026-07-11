using Application.UseCases.Wishlist.Requests;
using Domain.Repositories;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: delete an item from a user's wishlist.
/// </summary>
public class DeleteWishlistItemUseCase(
    IWishlistItemRepository wishlistItemRepository,
    IEventPublisher eventPublisher)
    : IDeleteWishlistItemUseCase
{
    public async Task ExecuteAsync(DeleteWishlistItemRequest request)
    {
        await wishlistItemRepository.DeleteWishlistItemAsync(request.UserId, request.AppId);

        await eventPublisher.PublishAsync(new Events.WishlistItemRemoved(
            request.UserId.ToString(),
            request.AppId,
            DateTimeOffset.UtcNow));
    }
}
