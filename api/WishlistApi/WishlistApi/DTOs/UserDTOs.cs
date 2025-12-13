namespace WishlistApi.DTOs
{
    public class UserDTOs
    {
        public record UserDetails(
            uint RowVersion,
            string Email,
            string? FirstName,
            string? LastName,
            string? Country,
            string? City,
            string? Address
        );
    }
}
