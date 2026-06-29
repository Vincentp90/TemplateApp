namespace Application.Contracts;

public record WishlistItemDto(
    int? AppId = null,
    DateTimeOffset? DateAdded = null,
    string? Name = null
);

public record Wishlist(IEnumerable<WishlistItemDto> Items);

public record Stats(
    TimeSpan AvgTimeAdded,
    TimeSpan AvgTimeBetweenAdded,
    string OldestItem,
    string MostCommonCharacter
);
