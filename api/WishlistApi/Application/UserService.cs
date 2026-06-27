using Application.Commands;
using Application.Contracts;
using Application.Queries;
using Domain.Helpers;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace Application
{
    public interface IUserService
    {
        ValueTask<int> GetInternalUserIdAsync(Guid guid);
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

    public class UserService(IUserRepository userRepo, IMemoryCache cache, IUnitOfWork unitOfWork, IUserReadModel userReadModel) : IUserService
    {
        public ValueTask<int> GetInternalUserIdAsync(Guid externalUserId)
        {
            if (cache.TryGetValue(externalUserId, out int id))
            {
                return new ValueTask<int>(id);
            }

            return GetInternalUserIdAsyncInternal(externalUserId);
        }

        private async ValueTask<int> GetInternalUserIdAsyncInternal(Guid externalUserId)
        {
            int id = await userRepo.GetInternalUserIdAsync(externalUserId);
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
            return await userReadModel.GetUsersAsync(page, limit);
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
