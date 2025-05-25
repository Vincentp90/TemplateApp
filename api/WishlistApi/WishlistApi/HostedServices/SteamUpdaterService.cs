using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using WishlistApi.Steam;

namespace WishlistApi.HostedServices
{
    /// <summary>
    /// Keep the list of steam games in the db up to date
    /// </summary>
    public class SteamUpdaterService : BackgroundService
    {
        private const string _steamApiUrl = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private readonly IServiceProvider _serviceProvider;

        public SteamUpdaterService(IServiceProvider serviceProvider) 
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateGameListingsIfEmpty(stoppingToken);

                // TODO add db table to keep track of last update
                // update existing data if longer than X ago

                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }

        //TODO move db specific code to DataAccess
        private async Task UpdateGameListingsIfEmpty(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();

            if (!await dbContext.GameListings.AnyAsync(stoppingToken))
            {
                var gameListings = await GetGameListingsFromSteam();
                if(gameListings == null)
                    throw new Exception("Failed to get game list from steam");
                gameListings.applist.apps = gameListings.applist.apps
                    .GroupBy(a => a.appid)
                    .Select(g => g.First())
                    .ToList();
                dbContext.GameListings.AddRange(gameListings.applist.apps);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }

        //TODO move to separate class or project, specific for getting data from steam
        private async Task<Root?> GetGameListingsFromSteam()
        {
            //Prod
            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(_steamApiUrl);
            var gameListings = JsonSerializer.Deserialize<Root>(response);
            //Dev
            //TODO read steamapplistExample.json
            return gameListings;
        }

        private class Root
        {
            public AppList applist { get; set; }
        }

        private class AppList
        {
            public List<GameListing> apps { get; set; }
        }
    }
}
