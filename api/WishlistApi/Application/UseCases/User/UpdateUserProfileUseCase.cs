using Application.UseCases.User.Requests;
using Microsoft.Extensions.Caching.Memory;
using Domain;
using Domain.Helpers;
using Domain.Repositories;

namespace Application.UseCases.User;

/// <summary>
/// Use case: update a user's profile details.
/// Resolves external UUID to internal ID using cached lookup.
/// </summary>
public class UpdateUserProfileUseCase(
    IUserRepository userRepo,
    IMemoryCache cache,
    IUnitOfWork unitOfWork)
    : IUpdateUserProfileUseCase
{
    public async Task ExecuteAsync(UpdateUserProfileRequest request)
    {
        int internalUserId = await GetInternalUserIdAsync(request.ExternalUserId);
        var user = await userRepo.GetUserAsync(internalUserId);
        if (user == null)
        {
            throw new Domain.Exceptions.NotFoundException("User not found");
        }

        user.UpdateDetails(request.Name, request.Location);
        await userRepo.UpdateUserAsync(user);
        await unitOfWork.SaveChangesAsync();
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
