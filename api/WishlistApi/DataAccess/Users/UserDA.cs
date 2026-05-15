using DataAccess.Auctions;
using DataAccess.Wishlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Users
{
    public interface IUserDA
    {
        Task<int> GetInternalUserIdAsync(Guid guid);
        //Task<User> GetUserAsync(int id);

        /// <summary>
        /// Get a page of users
        /// </summary>
        /// <param name="page">Which page</param>
        /// <param name="limit">How much users in one page</param>
        /// <returns>Limit + 1 users</returns>
        Task<List<User>> GetUsersAsync(int page, int limit);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<UserDetails> GetUserDetailsAsync(int userId);
        Task UpdateUserDetailsAsync(UserDetails userDetails);
    }

    public class UserDA(WishlistDbContext context, IMemoryCache cache) : IUserDA
    {
        public async Task<int> GetInternalUserIdAsync(Guid guid)
        {
            if (cache.TryGetValue(guid, out int id))
                return id;

            id = await context.Users.Where(u => u.UUID == guid).Select(u => u.ID).FirstAsync();

            cache.Set(guid, id, new MemoryCacheEntryOptions { Size = 1 });
            return id;
        }

        /*public async Task<User> GetUserAsync(int id)
        {
            return await _context.Users.Where(u => u.ID == id).FirstAsync();
        }*/

        
        public async Task<List<User>> GetUsersAsync(int page, int limit)
        {
            page = Math.Max(page, 1);
            limit = Math.Clamp(limit, 1, 200);
            return await context.Users.OrderBy(x => x.ID).Skip((page-1) * limit).Take(limit+1).ToListAsync();
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<UserDetails> GetUserDetailsAsync(int userId)
        {
            var details = await context.UserDetails.Include(u => u.User).FirstOrDefaultAsync(u => u.UserID == userId);
            if (details != null)
                return details;
            else
            {
                var user = await context.Users.FirstAsync(u => u.ID == userId);
                var userDetails = new UserDetails { UserID = user.ID, User = user };
                context.UserDetails.Add(userDetails);
                await context.SaveChangesAsync();
                return userDetails;
            }
        }

        public async Task UpdateUserDetailsAsync(UserDetails userDetails)
        {
            // Optimistic concurrency check
            context.Entry(userDetails).Property(a => a.RowVersion).OriginalValue = userDetails.RowVersion;

            context.UserDetails.Update(userDetails);
            await context.SaveChangesAsync();
        }
    }
}
