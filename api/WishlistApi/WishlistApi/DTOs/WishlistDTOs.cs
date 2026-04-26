using System.Dynamic;

namespace WishlistApi.DTOs
{
    public class WishlistDTOs
    {
        public record Wishlist(IEnumerable<ExpandoObject> Items);
    }
}
