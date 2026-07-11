namespace Application.UseCases.Auction.Requests;

/// <summary>
/// Request for placing a bid on an auction.
/// </summary>
public record PlaceBidRequest(
    int AuctionId,
    int UserId,
    decimal Amount,
    uint RowVersion);
