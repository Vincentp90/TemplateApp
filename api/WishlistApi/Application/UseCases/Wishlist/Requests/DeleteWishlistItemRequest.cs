namespace Application.UseCases.Wishlist.Requests;

/// <summary>
/// Request for deleting an item from a user's wishlist.
/// </summary>
public record DeleteWishlistItemRequest(int UserId, int AppId);
