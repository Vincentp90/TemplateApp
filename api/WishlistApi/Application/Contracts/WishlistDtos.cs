namespace Application.Contracts;

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
