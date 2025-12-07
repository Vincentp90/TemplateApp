using DataAccess;
using DataAccess.AppListings;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Buffers.Text;
using System.Text.Json;

namespace WishlistApi.HostedServices
{
    /// <summary>
    /// Keep the list of steam games in the db up to date
    /// </summary>
    public class SteamUpdaterService : BackgroundService
    {
        private const string _steamApiUrl = "https://api.steampowered.com/IStoreService/GetAppList/v1/";
        private readonly IServiceProvider _serviceProvider;

        public SteamUpdaterService(IServiceProvider serviceProvider) 
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateAppListingsIfEmptyAsync(stoppingToken);

                // TODO add db table to keep track of last update
                // update existing data if longer than X ago

                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }

        private async Task UpdateAppListingsIfEmptyAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            if (!await dbContext.AppListings.AnyAsync(stoppingToken))
            {
                var appListings = await GetAppListingsFromSteamAsyncV1(config["SteamAPIKEY"]!);
                if (appListings == null)
                    throw new Exception("Failed to get game list from steam");
                appListings.response.apps = appListings.response.apps
                    .GroupBy(a => a.appid)
                    .Select(g => g.First())
                    .ToList();
                dbContext.AppListings.AddRange(appListings.response.apps);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }

        private async Task<Root?> GetAppListingsFromSteamAsyncV1(string apiKey)
        {
            //Prod
            HttpClient client = new HttpClient();
            var url = QueryHelpers.AddQueryString(_steamApiUrl, new Dictionary<string, string?>
            {
                ["key"] = apiKey,
                ["max_results"] = "30000",
                ["include_dlc"] = "false",
            });
            var response = await client.GetStringAsync(url);
            var appListings = JsonSerializer.Deserialize<Root>(response);

            //Dev
            //TODO read steamapplistExample.json

            return appListings;
        }

        private class Root
        {
            public required AppList response { get; set; }
        }

        private class AppList
        {
            public required List<AppListing> apps { get; set; }
        }
    }
}
