using DataAccess.Users;
using System.Security.Claims;

namespace WishlistApi.Helpers
{
    public interface IUserContext
    {
        Task<int> GetIdAsync();
    }

    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserDA _userDA;
        private int? _cachedId;

        public UserContext(IHttpContextAccessor httpContextAccessor, IUserDA userDA)
        {
            _httpContextAccessor = httpContextAccessor;
            _userDA = userDA;
        }

        public async Task<int> GetIdAsync()
        {
            if (_cachedId.HasValue) return _cachedId.Value;

            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Unauthorized");

            _cachedId = await _userDA.GetInternalUserIdAsync(new Guid(userIdClaim));
            return _cachedId.Value;
        }
    }
}
