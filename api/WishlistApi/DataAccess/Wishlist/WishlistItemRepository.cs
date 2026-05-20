using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataAccess.Wishlist
{
    public class WishlistItemRepository(WishlistDbContext context) : IWishlistItemRepository
    {
        public async Task<List<Domain.WishlistItem>> GetWishlistItemsAsync(int userID)
        {
            var entities = await context.WishlistItems
                .Include(wi => wi.AppListing)
                .Where(wi => wi.UserID == userID)
                .ToListAsync();

            return entities.Select(MapToDomain).ToList();
        }

        public async Task<bool> AppIsOnWishlistAsync(int userID, int appid)
        {
            return await context.WishlistItems
                .Where(wi => wi.UserID == userID && wi.appid == appid)
                .AnyAsync();
        }

        public async Task AddWishlistItemAsync(Domain.WishlistItem item)
        {
            var entity = new WishlistItem
            {
                ID = item.Id, 
                appid = item.AppId, 
                DateAdded = item.DateAdded, 
                UserID = item.UserId
            };
            await context.WishlistItems.AddAsync(entity);
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

        private Domain.WishlistItem MapToDomain(WishlistItem entity)
        {
            var domain = new Domain.WishlistItem(
                entity.ID,
                entity.appid,
                entity.AppListing?.name ?? string.Empty,
                entity.DateAdded,
                entity.UserID
            );
            return domain;
        }
    }
}
