using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Wishlist
{
    public class WishlistItemDA
    {
        private readonly WishlistDbContext _context;

        public WishlistItemDA(WishlistDbContext dbContext)
        {
            _context = dbContext;
        }

        public List<WishlistItem> GetWishlistItems(string userid)
        {
            return _context.WishlistItems.Where(i => i.userid == userid).Include(i => i.AppListing).ToList();
        }

        public void AddWishlistItem(WishlistItem item)
        {
            item.dateadded = DateTimeOffset.UtcNow;//TODO pass client timezone and set in datetimeoffset DateTimeOffset.UtcNow.ToOffset(clientOffset);
            _context.WishlistItems.Add(item);
            _context.SaveChanges();
        }

        public void DeleteWishlistItem(string userid, int appid)
        {
            var item = _context.WishlistItems.FirstOrDefault(i => i.userid == userid && i.appid == appid);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                _context.SaveChanges();
            }
        }
    }
}
