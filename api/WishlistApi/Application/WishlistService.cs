using Application.Commands;
using DataAccess.Wishlist;
using Domain;
using Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var itemOnListAlready = await wishlistItemRepository.AppIsOnWishlistAsync(command.UserId, command.AppId);
            if (itemOnListAlready)
                throw new DuplicateNameException("Item already on wishlist");

            var wishlistItem = new Domain.WishlistItem(
                appId: command.AppId,
                dateAdded: DateTimeOffset.UtcNow,
                userId: command.UserId
            );

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

            if (!items.Any())
            {
                return new WishlistStats
                {
                    AvgTimeAdded = TimeSpan.Zero,
                    AvgTimeBetweenAdded = TimeSpan.Zero,
                    OldestItem = "",
                    MostCommonCharacter = ""
                };
            }

            var avgTicksAdded = items.Average(x => (DateTimeOffset.Now - x.DateAdded).Ticks);
            var avgTimeAdded = TimeSpan.FromTicks(Convert.ToInt64(avgTicksAdded));

            var orderedItems = items.OrderBy(x => x.Id);
            TimeSpan avgTimeBetweenAdded;
            if (items.Count() > 1)
            {
                // Overcomplicated original calculation
                //var avgTicksBetween = orderedItems.Zip(orderedItems.Skip(1), (a, b) => (b.DateAdded - a.DateAdded).Ticks).Average();
                // Much more simple and faster calculation:
                var totalSpanTicks = (orderedItems.Last().DateAdded - orderedItems.First().DateAdded).Ticks;
                var avgTicksBetween = totalSpanTicks / (orderedItems.Count() - 1);
                avgTimeBetweenAdded = TimeSpan.FromTicks(Convert.ToInt64(avgTicksBetween));
            }
            else
            {
                avgTimeBetweenAdded = TimeSpan.Zero;
            }            

            var oldestItem = orderedItems.FirstOrDefault()?.AppName ?? "";

            var appNamesConcatenated = items.SelectMany(x => x.AppName).Where(c => c != ' ');
            var mostCommonCharacter = appNamesConcatenated.GroupBy(x => x).MaxBy(x => x.Count())?.Key.ToString() ?? "";

            return new WishlistStats
            {
                AvgTimeAdded = avgTimeAdded,
                AvgTimeBetweenAdded = avgTimeBetweenAdded,
                OldestItem = oldestItem,
                MostCommonCharacter = mostCommonCharacter
            };
        }
    }
}
