namespace SteamTracker.Application.DTOs;

/// <summary>
/// DTO returned by GetWishlistWithPrices — joins TrackedGame + Game data.
/// </summary>
public record WishlistItemWithPriceDto(
    int AppId,
    string GameName,
    decimal? CurrentPrice,
    string Currency,
    bool IsFree,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset TrackedSince);
