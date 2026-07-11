using Application.Commands;
using Domain;
using Domain.Helpers;
using Domain.Repositories;

namespace Application
{
    public interface IWishlistService
    {
        Task<List<Domain.WishlistItem>> GetWishlistItemsAsync(int userID);
        Task AddToWishlistAsync(AddToWishlistCommand command);
        Task DeleteWishlistItemAsync(int userID, int appid);
        Task<WishlistStats> GetWishlistStatsAsync(int userID);
        Task PublishBackfillEventAsync(Domain.WishlistItem item);
    }

    public class WishlistService(
        IWishlistItemRepository wishlistItemRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher) : IWishlistService
    {
        public async Task<List<Domain.WishlistItem>> GetWishlistItemsAsync(int userID)
        {
            return await wishlistItemRepository.GetWishlistItemsAsync(userID);
        }

        public async Task AddToWishlistAsync(AddToWishlistCommand command)
        {
            if (await wishlistItemRepository.AppIsOnWishlistAsync(command.UserId, command.AppId))
                throw new Domain.Exceptions.DomainException("Item already on wishlist");

            var wishlistItem = Domain.WishlistItem.CreateNew(command.UserId, command.AppId);
            await wishlistItemRepository.AddWishlistItemAsync(wishlistItem);
            await unitOfWork.SaveChangesAsync();

            await eventPublisher.PublishAsync(new Events.WishlistItemAdded(
                command.UserId.ToString(),
                command.AppId,
                wishlistItem.DateAdded));
        }

        public async Task DeleteWishlistItemAsync(int userID, int appid)
        {
            await wishlistItemRepository.DeleteWishlistItemAsync(userID, appid);

            await eventPublisher.PublishAsync(new Events.WishlistItemRemoved(
                userID.ToString(),
                appid,
                DateTimeOffset.UtcNow));
        }

        public async Task<WishlistStats> GetWishlistStatsAsync(int userID)
        {
            var items = await wishlistItemRepository.GetWishlistItemsAsync(userID);
            return WishlistItem.CalculateStats(items);
        }

        public async Task PublishBackfillEventAsync(Domain.WishlistItem item)
        {
            await eventPublisher.PublishAsync(new Events.WishlistItemAdded(
                item.UserId.ToString(),
                item.AppId,
                item.DateAdded));
        }
    }
}
