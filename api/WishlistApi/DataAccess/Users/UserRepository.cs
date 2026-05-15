using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Users
{
    public class UserRepository(WishlistDbContext context) : IUserRepository
    {
        public async Task<Domain.User?> GetUserAsync(string username)
        {
            return await context.Users
                .Select(u => new Domain.User
                {
                    Id = u.ID,
                    Username = u.Username,
                    UUID = u.UUID,
                    PasswordHash = u.PasswordHash,
                    PasswordSalt = u.PasswordSalt,
                    Role = u.Role
                })
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public void AddUserAsync(Domain.User user)
        {
            var entity = new User
            {
                Username = user.Username,
                UUID = user.UUID,
                PasswordHash = user.PasswordHash,
                PasswordSalt = user.PasswordSalt,
                Role = user.Role
            };
            context.Users.Add(entity);
        }
    }
}
