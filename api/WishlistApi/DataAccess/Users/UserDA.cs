using DataAccess.Wishlist;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Users
{
    public class UserDA
    {
        private readonly WishlistDbContext _context;

        public UserDA(WishlistDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<bool> IsUsernameAvailable(string username)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task AddUser(string username, string password)
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

        public async Task<User?> LoginUser(string username, string password)
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
