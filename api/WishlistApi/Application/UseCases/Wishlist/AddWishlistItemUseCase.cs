using Application.UseCases.Wishlist.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: add an item to a user's wishlist.
/// </summary>
public class AddWishlistItemUseCase(
    IWishlistItemRepository wishlistItemRepository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher)
    : IAddWishlistItemUseCase
{
    public async Task ExecuteAsync(AddWishlistItemRequest request)
    {
        if (await wishlistItemRepository.AppIsOnWishlistAsync(request.UserId, request.AppId))
            throw new DomainException("Item already on wishlist");

        var wishlistItem = Domain.WishlistItem.CreateNew(request.UserId, request.AppId);
        await wishlistItemRepository.AddWishlistItemAsync(wishlistItem);
        await unitOfWork.SaveChangesAsync();

        await eventPublisher.PublishAsync(new Events.WishlistItemAdded(
            request.UserId.ToString(),
            request.AppId,
            wishlistItem.DateAdded));
    }
}
