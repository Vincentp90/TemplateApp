using Domain.Exceptions;
using Domain.Repositories;

namespace Domain;

public class WishlistItem
{
    public int Id { get; private set; }
    public int AppId { get; private set; }
    //TODO how to handle AppListing field
    public string AppName { get; private set; }
    public DateTimeOffset DateAdded { get; private set; }

    public int UserId { get; private set; }

    public WishlistItem(int id, int appId, string name, DateTimeOffset dateAdded, int userId)
    {
        Id = id;
        AppId = appId;
        AppName = name;
        DateAdded = dateAdded;
        UserId = userId;
    }

    public WishlistItem(int appId, DateTimeOffset dateAdded, int userId)
    {
        AppId = appId;
        DateAdded = dateAdded;
        UserId = userId;
        Id = 0;
        AppName = string.Empty;
    }

    public static async Task<WishlistItem> AddAsync(
        IWishlistItemRepository repository,
        int userId,
        int appId)
    {
        if (await repository.AppIsOnWishlistAsync(userId, appId))
            throw new DomainException("Item already on wishlist");

        return new WishlistItem(appId, DateTimeOffset.UtcNow, userId);
    }

    public static WishlistStats CalculateStats(IReadOnlyCollection<WishlistItem> items)
    {
        if (items == null || !items.Any())
        {
            return new WishlistStats(
                AvgTimeAdded: TimeSpan.Zero,
                AvgTimeBetweenAdded: TimeSpan.Zero,
                OldestItem: "",
                MostCommonCharacter: "");
        }

        var orderedItems = items.OrderBy(x => x.Id);
        var avgTicksAdded = items.Average(x => (DateTimeOffset.Now - x.DateAdded).Ticks);
        var avgTimeAdded = TimeSpan.FromTicks(Convert.ToInt64(avgTicksAdded));

        TimeSpan avgTimeBetweenAdded;
        if (orderedItems.Count() > 1)
        {
            var totalSpanTicks = (orderedItems.Last().DateAdded - orderedItems.First().DateAdded).Ticks;
            var avgTicksBetween = totalSpanTicks / (orderedItems.Count() - 1);
            avgTimeBetweenAdded = TimeSpan.FromTicks(Convert.ToInt64(avgTicksBetween));
        }
        else
        {
            avgTimeBetweenAdded = TimeSpan.Zero;
        }

        var oldestItem = orderedItems.FirstOrDefault()?.AppName ?? "";

        var appNamesConcatenated = items.SelectMany(x => x.AppName).Where(c => c != ' ');
        var mostCommonCharacter = appNamesConcatenated.GroupBy(x => x).MaxBy(x => x.Count())?.Key.ToString() ?? "";

        return new WishlistStats(
            AvgTimeAdded: avgTimeAdded,
            AvgTimeBetweenAdded: avgTimeBetweenAdded,
            OldestItem: oldestItem,
            MostCommonCharacter: mostCommonCharacter);
    }
}
