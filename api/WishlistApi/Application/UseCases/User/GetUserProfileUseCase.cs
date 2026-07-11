using Application.UseCases.User.Requests;
using Microsoft.Extensions.Caching.Memory;
using Domain.Repositories;

namespace Application.UseCases.User;

/// <summary>
/// Use case: retrieve a user's profile by external ID.
/// Resolves external UUID to internal ID using cached lookup.
/// </summary>
public class GetUserProfileUseCase(
    IUserRepository userRepo,
    IMemoryCache cache)
    : IGetUserProfileUseCase
{
    public async Task<Domain.User> ExecuteAsync(GetUserProfileRequest request)
    {
        int internalUserId = await GetInternalUserIdAsync(request.ExternalUserId);
        var user = await userRepo.GetUserAsync(internalUserId);
        if (user == null)
        {
            throw new Domain.Exceptions.NotFoundException("User not found");
        }
        return user;
    }

    private async Task<int> GetInternalUserIdAsync(Guid externalUserId)
    {
        if (cache.TryGetValue(externalUserId, out int id))
            return id;

        id = await userRepo.GetInternalUserIdAsync(externalUserId);
        cache.Set(externalUserId, id, new MemoryCacheEntryOptions { Size = 1 });
        return id;
    }
}
