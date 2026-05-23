namespace Application.Contracts
{
    public record WishlistStats(
        TimeSpan AvgTimeAdded,
        TimeSpan AvgTimeBetweenAdded,
        string OldestItem,
        string MostCommonCharacter
    );
}