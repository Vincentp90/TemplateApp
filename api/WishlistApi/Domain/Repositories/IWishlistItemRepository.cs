namespace Domain.Repositories;

public interface IWishlistItemRepository
{
    Task<List<WishlistItem>> GetWishlistItemsAsync(int userID);
    Task AddWishlistItemAsync(WishlistItem item);
    Task DeleteWishlistItemAsync(int userID, int appid);
    Task<bool> AppIsOnWishlistAsync(int userID, int appid);
}
