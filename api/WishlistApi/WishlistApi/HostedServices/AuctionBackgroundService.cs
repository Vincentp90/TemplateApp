using Application.UseCases.Auction;
using Application.UseCases.Auction.Requests;
using Domain;

namespace WishlistApi.HostedServices
{
    public class AuctionBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionBackgroundService> _logger;

        public AuctionBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AuctionBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Auction.Duration = TimeUntilNextHalfHour();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var startNextAuctionUseCase = scope.ServiceProvider.GetRequiredService<IStartNextAuctionUseCase>();

                    await startNextAuctionUseCase.ExecuteAsync(new StartNextAuctionRequest());
                }
                catch (Exception ex)
                {
                    // To deal with fresh start and fresh DB, GetRandomAppListingAsync will throw exception
                    // Just wait until next task run
                    _logger.LogError(ex, "AuctionBackgroundService encountered an error.");
                }

                await Task.Delay(Auction.Duration, stoppingToken);
                Auction.Duration = TimeUntilNextHalfHour();
            }
        }

        private TimeSpan TimeUntilNextHalfHour()
        {
            DateTime now = DateTime.Now;
            int minutesToWait = 30 - (now.Minute % 30);
            DateTime target = now.AddMinutes(minutesToWait)
                                 .AddSeconds(-now.Second)
                                 .AddMilliseconds(-now.Millisecond);
            return target - now;
        }
    }
}
