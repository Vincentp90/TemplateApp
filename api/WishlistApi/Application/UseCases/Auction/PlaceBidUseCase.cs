using Application.UseCases.Auction.Requests;
using Domain;
using Domain.Exceptions;
using Domain.Helpers;
using Domain.Repositories;

namespace Application.UseCases.Auction;

/// <summary>
/// Use case: place a bid on an open auction.
/// </summary>
public class PlaceBidUseCase(IAuctionRepository repository, IUnitOfWork unitOfWork) : IPlaceBidUseCase
{
    public async Task ExecuteAsync(PlaceBidRequest request)
    {
        var auction = await repository.GetOpenAuction(request.AuctionId);

        if (auction == null)
            throw new NotFoundException("Auction not found.");

        auction.PlaceBid(request.UserId, request.Amount);
        repository.Update(auction, request.RowVersion);

        await unitOfWork.SaveChangesAsync();
    }
}
