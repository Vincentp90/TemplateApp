using Domain;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;

namespace Application.UseCases.AppListing;

/// <summary>
/// Use case: ensure the app listings table is populated from Steam API.
/// Does nothing if listings already exist.
/// </summary>
public class EnsureAppListingsPopulatedUseCase(
    IAppListingRepository appListingRepository,
    ISteamApiClient steamApiClient,
    IConfiguration configuration)
    : IEnsureAppListingsPopulatedUseCase
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (await appListingRepository.HasAnyAsync())
            return;

        var apiKey = configuration["SteamAPIKEY"] ?? throw new InvalidOperationException("SteamAPIKEY configuration is missing.");
        var appListings = await steamApiClient.GetAppListingsAsync(apiKey);
        if (appListings == null)
            throw new Exception("Failed to get game list from steam");

        var distinctApps = appListings.Apps
            .GroupBy(a => a.AppId)
            .Select(g => g.First())
            .Select(a => new Domain.AppListing(a.AppId, a.Name))
            .ToList();

        await appListingRepository.SaveAsync(distinctApps);
    }
}
