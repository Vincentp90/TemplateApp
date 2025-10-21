namespace WishlistApi.DTOs
{
    public class AuthDTOs
    {
        public record RegisterRequest(string Username, string Password);
        public record LoginRequest(string Username, string Password);
        public record AuthResponse(string Token);
    }
}
