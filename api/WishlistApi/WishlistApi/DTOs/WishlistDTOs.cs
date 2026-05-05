using System.Dynamic;

namespace WishlistApi.DTOs
{
    public class WishlistDTOs
    {
        public record Wishlist(IEnumerable<ExpandoObject> Items);

        public record Stats(
            TimeSpan AvgTimeAdded,
            TimeSpan AvgTimeBetweenAdded,
            string OldestItem,
            string MostCommonCharacter
            );
    }
}
