using Application.Commands;
using DataAccess.Users;
using DataAccess.Wishlist;
using Domain.Helpers;
using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IUserService
    {
        Task<int> GetInternalUserIdAsync(Guid guid);
        /// <summary>
        /// Get a page of users
        /// </summary>
        /// <param name="page">Which page</param>
        /// <param name="limit">How much users in one page</param>
        /// <returns>Limit + 1 users</returns>
        Task<List<User>> GetUsersAsync(int page, int limit);
        Task<Domain.User> GetUserAsync(GetUserCommand command);
        Task UpdateUserDetailsAsync(UpdateUserDetailsCommand command);
    }

    public class UserService(IUserDA userDA, IUserRepository userRepo, IUnitOfWork unitOfWork) : IUserService
    {
        public async Task<int> GetInternalUserIdAsync(Guid guid)
        {
            return await userDA.GetInternalUserIdAsync(guid);
        }

        public async Task<Domain.User> GetUserAsync(GetUserCommand command)
        {
            int internalUserId = await GetInternalUserIdAsync(command.ExternalUserId);
            return await userRepo.GetUserAsync(internalUserId);
        }

        public async Task<List<User>> GetUsersAsync(int page, int limit)
        {
            return await userDA.GetUsersAsync(page, limit);
        }

        public async Task UpdateUserDetailsAsync(UpdateUserDetailsCommand command)
        {
            int internalUserId = await GetInternalUserIdAsync(command.ExternalUserId);
            var user = await userRepo.GetUserAsync(internalUserId);
            
            user.UpdateDetails(
                command.FirstName,
                command.LastName,
                command.Country,
                command.City,
                command.Address);

            await userRepo.UpdateUserAsync(user);

            await unitOfWork.SaveChangesAsync();
        }
    }
}
