using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Users
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

        private static readonly Expression<Func<Infrastructure.Persistence.Users.UserDetails, Domain.User>> ToDomain =
            u => new Domain.User(
                id: u.ID,
                username: u.User.Username,
                uuid: u.User.UUID,
                passwordHash: u.User.PasswordHash,
                passwordSalt: u.User.PasswordSalt,
                role: u.User.Role,
                details: new Domain.UserDetails(
                    name: new FullName(u.FirstName, u.LastName),
                    address: new Address(u.Country, u.City, u.Address),
                    rowVersion: u.RowVersion
                )
            );

        public void AddUser(Domain.User user)
        {
            var entity = new User
            {
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                PasswordSalt = user.PasswordSalt,
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

            entity.FirstName = user.Details.Name.FirstName;
            entity.LastName = user.Details.Name.LastName;
            entity.Country = user.Details.Location.Country;
            entity.City = user.Details.Location.City;
            entity.Address = user.Details.Location.Street;

            // We don't update User fields because they should be immutable except password
            // Changing password has separate flow (doesn't exist atm)

            // Optimistic concurrency check
            context.Entry(entity).Property(a => a.RowVersion).OriginalValue = user.Details.RowVersion;
        }

        public async Task<int> GetInternalUserIdAsync(Guid externalUserId)
        {
            return await context.Users
                .Where(u => u.UUID == externalUserId)
                .Select(u => u.ID)
                .FirstAsync();
        }
    }
}
