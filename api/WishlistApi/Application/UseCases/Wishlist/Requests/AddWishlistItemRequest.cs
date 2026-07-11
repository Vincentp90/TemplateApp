namespace Application.UseCases.Wishlist.Requests;

/// <summary>
/// Request for adding an item to a user's wishlist.
/// </summary>
public record AddWishlistItemRequest(int UserId, int AppId);
