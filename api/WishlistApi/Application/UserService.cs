using Application.Commands;
using Application.Contracts;
using Application.Queries;
using Domain.Helpers;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Persistence.Users;
using Infrastructure.Persistence.Wishlist;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

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
        Task<List<UserSummaryDto>> GetUsersAsync(int page, int limit);
        Task<Domain.User> GetUserAsync(GetUserCommand command);
        Task UpdateUserDetailsAsync(UpdateUserDetailsCommand command);
    }

    public class UserService(IUserRepository userRepo, IMemoryCache cache, IUnitOfWork unitOfWork, IUserQueries userQueries) : IUserService
    {
        //TODO this should be ValueTask because then cache hits will be faster, synchronous execution will be without Task wrapping
        public async Task<int> GetInternalUserIdAsync(Guid externalUserId)
        {
            if (cache.TryGetValue(externalUserId, out int id))
                return id;

            id = await userRepo.GetInternalUserIdAsync(externalUserId);

            cache.Set(externalUserId, id, new MemoryCacheEntryOptions { Size = 1 });
            return id;
        }

        public async Task<Domain.User> GetUserAsync(GetUserCommand command)
        {
            int internalUserId = await GetInternalUserIdAsync(command.ExternalUserId);
            return await userRepo.GetUserAsync(internalUserId);
        }

        public async Task<List<UserSummaryDto>> GetUsersAsync(int page, int limit)
        {
            return await userQueries.GetUsersAsync(page, limit);
        }

        public async Task UpdateUserDetailsAsync(UpdateUserDetailsCommand command)
        {
            int internalUserId = await GetInternalUserIdAsync(command.ExternalUserId);
            var user = await userRepo.GetUserAsync(internalUserId);

            user.UpdateDetails(command.Name, command.Location);

            await userRepo.UpdateUserAsync(user);

            await unitOfWork.SaveChangesAsync();
        }
    }
}
