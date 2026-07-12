using Domain.Repositories;
using System.Security.Claims;

namespace WishlistApi.Helpers
{
    public interface IUserContext
    {
        ValueTask<int> GetIdAsync();
    }

    public class UserContext(IHttpContextAccessor httpContextAccessor, IUserRepository userRepo) : IUserContext
    {
        private int? _cachedId;

        // Task vs ValueTask performance, respectively for cache miss, memorycache hit, cachedId hit
        // Task:        1.10 ms, 8.33 µs, 10.46 ns
        // ValueTask:   1.05 ms, 8.64 µs,  6.06 ns
        // ValueTask is clearly faster when hitting cachedId, for the memorycache case it's probably negligible compared to the other overhead when doing a memorycache lookup
        public ValueTask<int> GetIdAsync()
        {
            if (_cachedId.HasValue)
            {
                return new ValueTask<int>(_cachedId.Value);
            }

            return GetIdAsyncInternal();
        }

        private async ValueTask<int> GetIdAsyncInternal()
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Unauthorized");

            _cachedId = await userRepo.GetInternalUserIdAsync(new Guid(userIdClaim));
            return _cachedId.Value;
        }
    }
}
