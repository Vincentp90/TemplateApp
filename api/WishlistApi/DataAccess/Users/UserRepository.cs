using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DataAccess.Users
{
    public class UserRepository(WishlistDbContext context) : IUserRepository
    {
        public async Task<Domain.User?> GetUserAsync(string username)
        {
            return await context.UserDetails
                .Include(u => u.User)
                .Where(u => u.User.Username == username)
                .Select(ToDomain)
                .FirstOrDefaultAsync();
        }

        public async Task<Domain.User> GetUserAsync(int id)
        {
            return await context.UserDetails
                .Include(u => u.User)
                .Where(u => u.ID == id)
                .Select(ToDomain)
                .FirstAsync();
        }

        private static readonly Expression<Func<UserDetails, Domain.User>> ToDomain =
            u => new Domain.User(
                id: u.ID,
                username: u.User.Username,
                uuid: u.User.UUID,
                passwordHash: u.User.PasswordHash,
                passwordSalt: u.User.PasswordSalt,
                role: u.User.Role,
                details: new Domain.UserDetails(
                    firstName: u.FirstName,
                    lastName: u.LastName,
                    country: u.Country,
                    city: u.City,
                    address: u.Address,
                    rowVersion: u.RowVersion
                )
            );

        public void AddUser(Domain.User user)
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

            var userDetails = new UserDetails { UserID = user.Id, User = entity };
            context.UserDetails.Add(userDetails);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task UpdateUserAsync(Domain.User user)
        {
            var entity = await context.UserDetails.Include(u => u.User).FirstAsync(u => u.ID == user.Id);

            entity.FirstName = user.Details.FirstName;
            entity.LastName = user.Details.LastName;
            entity.Country = user.Details.Country;
            entity.City = user.Details.City;
            entity.Address = user.Details.Address;

            // We don't update User fields because they should be immutable except password
            // Changing password has separate flow (doesn't exist atm)

            // Optimistic concurrency check
            context.Entry(entity).Property(a => a.RowVersion).OriginalValue = user.Details.RowVersion;
        }
    }
}
