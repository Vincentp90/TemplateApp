namespace Application.Events;

/// <summary>
/// Published when a user removes a game from their wishlist.
/// Only UserId and AppId are included.
/// </summary>
public record WishlistItemRemoved(string UserId, int AppId, DateTimeOffset RemovedAt);
