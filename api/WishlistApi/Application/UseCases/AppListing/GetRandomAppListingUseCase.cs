using Domain.Repositories;

namespace Application.UseCases.AppListing;

/// <summary>
/// Use case: retrieve a random app listing.
/// </summary>
public class GetRandomAppListingUseCase(IAppListingRepository appListingRepository) : IGetRandomAppListingUseCase
{
    public async Task<Domain.AppListing> ExecuteAsync(UnitRequest request)
    {
        return await appListingRepository.GetRandomAsync();
    }
}

/// <summary>
/// Marker request for use cases that take no parameters.
/// </summary>
public record UnitRequest;
