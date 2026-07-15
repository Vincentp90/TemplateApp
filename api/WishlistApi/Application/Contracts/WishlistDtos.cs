namespace Application.Contracts;

/// <summary>
/// DTO for returning game price information from SteamTracker passthrough.
/// </summary>
public record GamePriceDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable);

public record WishlistItemDto(
    int? AppId = null,
    DateTimeOffset? DateAdded = null,
    string? Name = null,
    decimal? Price = null,
    string? PriceCurrency = "EUR",
    DateTimeOffset? LastCheckedAt = null,
    bool IsUnavailable = false,
    Guid? AlertRuleId = null,
    decimal? AlertThreshold = null,
    string? AlertCurrency = "EUR"
);

public record Wishlist(IEnumerable<WishlistItemDto> Items);

public record Stats(
    TimeSpan AvgTimeAdded,
    TimeSpan AvgTimeBetweenAdded,
    string OldestItem,
    string MostCommonCharacter
);
