using Application.UseCases.AppListing;
using Application.UseCases.Auction.Requests;
using Domain;
using Domain.Helpers;
using Domain.Repositories;

namespace Application.UseCases.Auction;

/// <summary>
/// Use case: start the next auction by picking a random game listing.
/// </summary>
public class StartNextAuctionUseCase(
    IAuctionRepository repository,
    IUnitOfWork unitOfWork,
    IGetRandomAppListingUseCase getRandomAppListingUseCase)
    : IStartNextAuctionUseCase
{
    public async Task ExecuteAsync(StartNextAuctionRequest request = default!)
    {
        var app = await getRandomAppListingUseCase.ExecuteAsync(new UnitRequest());

        var newAuction = Domain.Auction.CreateNext(app.Id);

        var latestAuction = await repository.GetLatestAuctionAsync();
        if (latestAuction != null)
        {
            await repository.CloseAuctionAndAddNewAsync(newAuction);
        }
        else
        {
            repository.AddAuction(newAuction);
        }
        await unitOfWork.SaveChangesAsync();
    }
}
