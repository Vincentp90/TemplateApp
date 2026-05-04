using Application.Domain;
using DataAccess.Wishlist;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Wishlist
{
    public interface IWishlistService
    {
        Task<WishlistStats> GetWishlistStats(int userID);
    }

    public class WishlistService
    {
        //private readonly IWishlistRepository _wishlistRepository;TODO
        private readonly IWishlistItemDA _wishlistItemDA;

        public WishlistService(IWishlistItemDA wishlistItemDA)
        {
            _wishlistItemDA = wishlistItemDA;
        }

        public async Task<WishlistStats> GetWishlistStats(int userID)
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
                var avgTicksBetween = orderedItems.Zip(orderedItems.Skip(1), (a, b) => (b.DateAdded - a.DateAdded).Ticks).Average();
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
