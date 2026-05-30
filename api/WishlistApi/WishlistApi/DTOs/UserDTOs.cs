namespace WishlistApi.DTOs
{
    public record UserDetailsDTO(
        uint RowVersion,
        string Email,
        string? FirstName,
        string? LastName,
        string? Country,
        string? City,
        string? Address
    );
}
