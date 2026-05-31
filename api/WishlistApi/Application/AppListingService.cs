using DataAccess.AppListings;
using Microsoft.Extensions.Configuration;
using Domain;
using AppListingEF = DataAccess.AppListings.AppListing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application
{
    public interface IAppListingService
    {
        Task<List<AppListingEF>> SearchAppListingsAsync(string term);
        Task<AppListingEF> GetRandomAppListingAsync();
        Task EnsureAppListingsPopulatedAsync(CancellationToken cancellationToken = default);
    }

    public class AppListingService : IAppListingService
    {
        private readonly IAppListingDA _appListingDA;
        private readonly ISteamApiClient _steamApiClient;
        private readonly IConfiguration _configuration;

        public AppListingService(IAppListingDA appListingDA, ISteamApiClient steamApiClient, IConfiguration configuration)
        {
            _appListingDA = appListingDA;
            _steamApiClient = steamApiClient;
            _configuration = configuration;
        }

        public async Task<AppListingEF> GetRandomAppListingAsync()
        {
            return await _appListingDA.GetRandomAppListingAsync();
        }

        public async Task<List<AppListingEF>> SearchAppListingsAsync(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 3)
                return new List<AppListingEF>();
            return await _appListingDA.SearchAppListingsAsync(term);
        }

        public async Task EnsureAppListingsPopulatedAsync(CancellationToken cancellationToken = default)
        {
            if (await _appListingDA.HasAnyAsync())
                return;

            var appListings = await _steamApiClient.GetAppListingsAsync(_configuration["SteamAPIKEY"]!);
            if (appListings == null)
                throw new Exception("Failed to get game list from steam");

            var distinctApps = appListings.Apps
                .GroupBy(a => a.AppId)
                .Select(g => g.First())
                .Select(a => new AppListingEF { appid = a.AppId, name = a.Name })
                .ToList();

            await _appListingDA.SaveAppListingsAsync(distinctApps);
        }
    }
}
