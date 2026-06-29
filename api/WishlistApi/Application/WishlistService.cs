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
    }

    public class WishlistService(IWishlistItemRepository wishlistItemRepository, IUnitOfWork unitOfWork) : IWishlistService
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
        }

        public async Task DeleteWishlistItemAsync(int userID, int appid)
        {
            await wishlistItemRepository.DeleteWishlistItemAsync(userID, appid);
        }

        public async Task<WishlistStats> GetWishlistStatsAsync(int userID)
        {
            var items = await wishlistItemRepository.GetWishlistItemsAsync(userID);
            return WishlistItem.CalculateStats(items);
        }
    }
}
