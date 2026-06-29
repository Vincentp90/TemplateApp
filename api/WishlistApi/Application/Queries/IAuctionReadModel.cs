namespace Application.Queries;

public interface IAuctionReadModel
{
    Task<Contracts.AuctionDto?> GetCurrentAuctionAsync(Guid? currentUserGuid);
}
