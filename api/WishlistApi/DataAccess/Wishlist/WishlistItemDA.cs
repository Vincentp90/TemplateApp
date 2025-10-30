using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Wishlist
{
    public interface IWishlistItemDA
    {
        Task<List<WishlistItem>> GetWishlistItems(int userID);
        Task AddWishlistItem(WishlistItem item);
        Task DeleteWishlistItem(int userID, int appid);
    }

    public class WishlistItemDA : IWishlistItemDA
    {
        private readonly WishlistDbContext _context;

        public WishlistItemDA(WishlistDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<List<WishlistItem>> GetWishlistItems(int userID)
        {
            return await _context.WishlistItems.Where(i => i.UserID == userID).Include(i => i.AppListing).ToListAsync();
        }

        public async Task AddWishlistItem(WishlistItem item)
        {
            item.DateAdded = DateTimeOffset.UtcNow;//TODO pass client timezone and set in datetimeoffset DateTimeOffset.UtcNow.ToOffset(clientOffset);
            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteWishlistItem(int userID, int appid)
        {
            var item = _context.WishlistItems.FirstOrDefault(i => i.UserID == userID && i.appid == appid);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
