using SteamTracker.Domain.ValueObjects;

namespace SteamTracker.Worker;

/// <summary>
/// Message sent to the price-check job queue.
/// </summary>
public record PriceCheckMessage(int AppId, DateTimeOffset EnqueuedAt);

/// <summary>
/// Message representing a wishlist item added event from the ACL exchange.
/// </summary>
public record WishlistItemAddedMessage(string UserId, int AppId, DateTimeOffset AddedAt);

/// <summary>
/// Message representing a wishlist item removed event from the ACL exchange.
/// </summary>
public record WishlistItemRemovedMessage(string UserId, int AppId, DateTimeOffset RemovedAt);
