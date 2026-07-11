namespace Application.Events;

/// <summary>
/// Published when a user adds a game to their wishlist.
/// Only UserId and AppId are included — SteamTracker resolves the game name from the Steam API.
/// </summary>
public record WishlistItemAdded(string UserId, int AppId, DateTimeOffset AddedAt);
