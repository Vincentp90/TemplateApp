using Application.Domain;
using DataAccess.Wishlist;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IWishlistService
    {
        Task<List<WishlistItem>> GetWishlistItemsAsync(int userID);
        Task AddWishlistItemAsync(WishlistItem item);
        Task DeleteWishlistItemAsync(int userID, int appid);
        Task<WishlistStats> GetWishlistStatsAsync(int userID);
        
    }

    public class WishlistService : IWishlistService
    {
        //private readonly IWishlistRepository _wishlistRepository;TODO
        private readonly IWishlistItemDA _wishlistItemDA;

        public WishlistService(IWishlistItemDA wishlistItemDA)
        {
            _wishlistItemDA = wishlistItemDA;
        }        

        public async Task<List<WishlistItem>> GetWishlistItemsAsync(int userID)
        {
            return await _wishlistItemDA.GetWishlistItemsAsync(userID);
        }

        public async Task AddWishlistItemAsync(WishlistItem item)
        {
            await _wishlistItemDA.AddWishlistItemAsync(item);
        }

        public async Task DeleteWishlistItemAsync(int userID, int appid)
        {
            await _wishlistItemDA.DeleteWishlistItemAsync(userID, appid);
        }

        public async Task<WishlistStats> GetWishlistStatsAsync(int userID)
        {
            var items = await _wishlistItemDA.GetWishlistItemsAsync(userID);
            WishlistStats stats = new WishlistStats();

            if (!items.Any())
            {
                stats.AvgTimeAdded = TimeSpan.Zero;
                stats.AvgTimeBetweenAdded = TimeSpan.Zero;
                stats.OldestItem = "";
                stats.MostCommonCharacter = "";
                return stats;
            }

            var avgTicksAdded = items.Average(x => (DateTimeOffset.Now - x.DateAdded).Ticks);
            stats.AvgTimeAdded = TimeSpan.FromTicks(Convert.ToInt64(avgTicksAdded));

            var orderedItems = items.OrderBy(x => x.ID);
            if (items.Count() > 1)
            {
                // Overcomplicated original calculation
                //var avgTicksBetween = orderedItems.Zip(orderedItems.Skip(1), (a, b) => (b.DateAdded - a.DateAdded).Ticks).Average();
                // Much more simple and faster calculation:
                var totalSpanTicks = (orderedItems.Last().DateAdded - orderedItems.First().DateAdded).Ticks;
                var avgTicksBetween = totalSpanTicks / (orderedItems.Count() - 1);
                stats.AvgTimeBetweenAdded = TimeSpan.FromTicks(Convert.ToInt64(avgTicksBetween));
            }
            else
            {
                stats.AvgTimeBetweenAdded = TimeSpan.Zero;
            }            

            stats.OldestItem = orderedItems.FirstOrDefault()?.AppListing!.name ?? "";

            var appNamesConcatenated = items.SelectMany(x => x.AppListing!.name).Where(c => c != ' ');
            stats.MostCommonCharacter = appNamesConcatenated.GroupBy(x => x).MaxBy(x => x.Count())?.Key.ToString() ?? "";

            return stats;
        }
    }
}
