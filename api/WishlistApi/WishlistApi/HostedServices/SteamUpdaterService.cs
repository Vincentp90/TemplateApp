using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace WishlistApi.HostedServices
{
    /// <summary>
    /// Keep the list of steam games in the db up to date
    /// </summary>
    public class SteamUpdaterService : BackgroundService
    {
        private const string _steamApiUrl = "https://api.steampowered.com/IStoreService/GetAppList/v1/";
        private readonly IServiceProvider _serviceProvider;
        private readonly ISteamApiClient _steamApiClient;

        public SteamUpdaterService(IServiceProvider serviceProvider, ISteamApiClient steamApiClient)
        {
            _serviceProvider = serviceProvider;
            _steamApiClient = steamApiClient;
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

        internal async Task UpdateAppListingsIfEmptyAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();// TODO only direct reference to DA layer left, nice way to fix?
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            if (!await dbContext.AppListings.AnyAsync(stoppingToken))
            {
                var appListings = await _steamApiClient.GetAppListingsAsync(config["SteamAPIKEY"]!);
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
    }
}
