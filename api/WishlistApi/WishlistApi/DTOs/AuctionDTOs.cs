namespace WishlistApi.DTOs
{
    public class AuctionDTOs
    {
        public record Auction(int ID, DateTimeOffset StartDate, DateTimeOffset EndDate, bool UserHasBid, decimal StartingPrice, decimal? CurrentPrice, string AppName, Guid RowVersion);
    }
}
