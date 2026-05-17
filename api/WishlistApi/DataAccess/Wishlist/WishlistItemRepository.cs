using Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataAccess.Wishlist
{
    public interface IWishlistItemRepository
    {
        Task<List<WishlistItem>> GetWishlistItemsAsync(int userID);
        Task AddWishlistItemAsync(WishlistItem item);
        Task DeleteWishlistItemAsync(int userID, int appid);
        Task<bool> AppIsOnWishlistAsync(int userID, int appid);
    }

    public class WishlistRepository(WishlistDbContext context) : IWishlistItemRepository
    {
        public async Task<List<WishlistItem>> GetWishlistItemsAsync(int userID)
        {
            return await context.WishlistItems
                .Include(wi => wi.AppListing)
                .Where(wi => wi.UserID == userID)
                .ToListAsync();
        }

        public async Task<bool> AppIsOnWishlistAsync(int userID, int appid)
        {
            return await context.WishlistItems
                .Where(wi => wi.UserID == userID && wi.appid == appid)
                .AnyAsync();
        }

        public async Task AddWishlistItemAsync(WishlistItem item)
        {
            await context.WishlistItems.AddAsync(item);
        }

        public async Task DeleteWishlistItemAsync(int userID, int appid)
        {
            var item = await context.WishlistItems
                .FirstOrDefaultAsync(wi => wi.UserID == userID && wi.appid == appid);

            if (item != null)
            {
                context.WishlistItems.Remove(item);
                await context.SaveChangesAsync();
            }
        }
    }
}
