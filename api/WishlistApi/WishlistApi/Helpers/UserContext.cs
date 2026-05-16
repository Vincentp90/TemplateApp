using Application;
using System.Security.Claims;

namespace WishlistApi.Helpers
{
    public interface IUserContext
    {
        Task<int> GetIdAsync();
    }

    public class UserContext(IHttpContextAccessor httpContextAccessor, IUserService userService) : IUserContext
    {
        private int? _cachedId;

        public async Task<int> GetIdAsync()
        {
            if (_cachedId.HasValue) return _cachedId.Value;

            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Unauthorized");

            _cachedId = await userService.GetInternalUserIdAsync(new Guid(userIdClaim));
            return _cachedId.Value;
        }
    }
}
