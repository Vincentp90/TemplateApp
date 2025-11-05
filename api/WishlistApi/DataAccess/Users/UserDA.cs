using DataAccess.Wishlist;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace DataAccess.Users
{
    public interface IUserDA
    {
        Task<int> GetInternalUserIdAsync(Guid guid);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task AddUserAsync(string username, string password);
        Task<User?> LoginUserAsync(string username, string password);
    }

    public class UserDA : IUserDA
    {
        private readonly WishlistDbContext _context;
        private readonly IMemoryCache _cache;

        public UserDA(WishlistDbContext dbContext, IMemoryCache cache)
        {
            _context = dbContext;
            _cache = cache;
        }

        public async Task<int> GetInternalUserIdAsync(Guid guid)
        {
            if (_cache.TryGetValue(guid, out int id))
                return id;

            id = await _context.Users.Where(u => u.UUID == guid).Select(u => u.ID).FirstAsync();

            _cache.Set(guid, id, new MemoryCacheEntryOptions { Size = 1 });
            return await _context.Users.Where(u => u.UUID == guid).Select(u => u.ID).FirstAsync();
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task AddUserAsync(string username, string password)
        {
            CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> LoginUserAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return null;

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;
            return user;
        }

        private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using var hmac = new Rfc2898DeriveBytes(password, 16, 100_000, HashAlgorithmName.SHA256);
            salt = hmac.Salt;
            hash = hmac.GetBytes(32);
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new Rfc2898DeriveBytes(password, storedSalt, 100_000, HashAlgorithmName.SHA256);
            var computed = hmac.GetBytes(32);
            return computed.SequenceEqual(storedHash);
        }        
    }
}
