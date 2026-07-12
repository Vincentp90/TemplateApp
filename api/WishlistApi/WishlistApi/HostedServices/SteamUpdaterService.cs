using Application.UseCases.AppListing;
using System.Threading;
using System.Threading.Tasks;

namespace WishlistApi.HostedServices
{
    /// <summary>
    /// Keep the list of steam games in the db up to date
    /// </summary>
    public class SteamUpdaterService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SteamUpdaterService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var ensureAppListingsPopulatedUseCase = scope.ServiceProvider.GetRequiredService<IEnsureAppListingsPopulatedUseCase>();
                await ensureAppListingsPopulatedUseCase.ExecuteAsync(stoppingToken);

                // TODO add db table to keep track of last update
                // update existing data if longer than X ago

                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }
    }
}
