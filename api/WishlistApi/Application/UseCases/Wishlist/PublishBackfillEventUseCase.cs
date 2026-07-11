using Application.Events;
using Application.UseCases.Wishlist.Requests;

namespace Application.UseCases.Wishlist;

/// <summary>
/// Use case: publish a WishlistItemAdded event for backfill purposes.
/// </summary>
public class PublishBackfillEventUseCase(IEventPublisher eventPublisher) : IPublishBackfillEventUseCase
{
    public async Task ExecuteAsync(PublishBackfillEventRequest request)
    {
        await eventPublisher.PublishAsync(new WishlistItemAdded(
            request.UserId.ToString(),
            request.AppId,
            request.DateAdded));
    }
}
