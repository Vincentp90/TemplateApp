using DataAccess.AppListings;
using DataAccess.Auctions;

namespace WishlistApi.HostedServices
{
    public class AuctionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionService> _logger;

        public AuctionService(IServiceScopeFactory scopeFactory, ILogger<AuctionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var _auctionDA = scope.ServiceProvider.GetRequiredService<IAuctionDA>();
                    var _appListingDA = scope.ServiceProvider.GetRequiredService<IAppListingDA>();

                    var app = await _appListingDA.GetRandomAppListingAsync();

                    var newAuction = new Auction()
                    {
                        DateAdded = DateTimeOffset.UtcNow,
                        Status = AuctionStatus.Open,
                        AppListing = app,
                        appid = app.appid,
                        StartingPrice = 1.0m,
                    };

                    var latestAuction = await _auctionDA.GetLatestAuctionAsync();
                    if (latestAuction != null)
                    {
                        await _auctionDA.CloseAuctionAndAddNewAsync(latestAuction, newAuction);
                        _logger.LogInformation("Closed auction ID={ID} appName={AppName}. New auction ID={ID} appName={NewAppName}", latestAuction.ID, latestAuction.AppListing.name, newAuction.ID, app.name);
                    }
                    else
                    {
                        await _auctionDA.AddAuctionAsync(newAuction);
                        _logger.LogInformation("New auction ID={ID} appName={AppName}", newAuction.ID, app.name);
                    }
                }
                catch (Exception ex)
                {
                    // To deal with fresh start and fresh DB, GetRandomAppListingAsync will throw exception
                    // Just wait until next task run
                    _logger.LogError(ex, "AuctionService encountered an error.");
                }

                //TODO instead of wait 30 minutes, wait until next half or full hour
                await Task.Delay(Auction.Duration, stoppingToken);
            }
        }
    }
}
