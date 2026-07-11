using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application
{
    public interface IAppListingService
    {
        Task<List<Contracts.AppListingDto>> SearchAppListingsAsync(string term);
        Task<Domain.AppListing> GetRandomAppListingAsync();
        Task EnsureAppListingsPopulatedAsync(CancellationToken cancellationToken = default);
    }

    public class AppListingService : IAppListingService
    {
        private readonly IAppListingRepository _appListingRepository;
        private readonly ISteamApiClient _steamApiClient;
        private readonly IConfiguration _configuration;

        public AppListingService(IAppListingRepository appListingRepository, ISteamApiClient steamApiClient, IConfiguration configuration)
        {
            _appListingRepository = appListingRepository;
            _steamApiClient = steamApiClient;
            _configuration = configuration;
        }

        public async Task<Domain.AppListing> GetRandomAppListingAsync()
        {
            return await _appListingRepository.GetRandomAsync();
        }

        public async Task<List<Contracts.AppListingDto>> SearchAppListingsAsync(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 3)
                return new List<Contracts.AppListingDto>();
            var results = await _appListingRepository.SearchAsync(term);
            return results.Select(a => new Contracts.AppListingDto(a.Id, a.Name)).ToList();
        }

        public async Task EnsureAppListingsPopulatedAsync(CancellationToken cancellationToken = default)
        {
            if (await _appListingRepository.HasAnyAsync())
                return;

            var appListings = await _steamApiClient.GetAppListingsAsync(_configuration["SteamAPIKEY"]!);
            if (appListings == null)
                throw new Exception("Failed to get game list from steam");

            var distinctApps = appListings.Apps
                .GroupBy(a => a.AppId)
                .Select(g => g.First())
                .Select(a => new Domain.AppListing(a.AppId, a.Name))
                .ToList();

            await _appListingRepository.SaveAsync(distinctApps);
        }
    }
}
