using Application;
using System.Threading;
using System.Threading.Tasks;

namespace WishlistApi.HostedServices
{
    /// <summary>
    /// Keep the list of steam games in the db up to date
    /// </summary>
    public class SteamUpdaterService : BackgroundService
    {
        private readonly IAppListingService _appListingService;

        public SteamUpdaterService(IAppListingService appListingService)
        {
            _appListingService = appListingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _appListingService.EnsureAppListingsPopulatedAsync(stoppingToken);

                // TODO add db table to keep track of last update
                // update existing data if longer than X ago

                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }
    }
}
