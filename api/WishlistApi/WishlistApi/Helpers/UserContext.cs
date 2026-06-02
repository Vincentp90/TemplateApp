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

        //TODO this should be ValueTask because then cache hits will be faster, synchronous execution will be without Task wrapping
        public async Task<int> GetIdAsync()
        {
            // Not sure if this caching here has a point, we already have a memory cache in GetInternalUserIdAsync
            if (_cachedId.HasValue) return _cachedId.Value;

            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Unauthorized");

            _cachedId = await userService.GetInternalUserIdAsync(new Guid(userIdClaim));
            return _cachedId.Value;
        }
    }
}
