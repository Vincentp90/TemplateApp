using DataAccess.AppListings;
using DataAccess.Users;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using System.Security.Claims;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistItemDA _wishlistItemDA;
        private readonly IUserDA _userDA;

        public WishlistController(IWishlistItemDA wishlistItemDA, IUserDA userDA)
        {
            _wishlistItemDA = wishlistItemDA;
            _userDA = userDA;
        }

        [HttpGet()]
        public async Task<ActionResult> GetWishlist([FromQuery] string? fields = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserId(new Guid(userId));

            var fieldList = (fields ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(f => f.ToLower())
                                .ToHashSet();
            bool includeAll = fieldList.Count == 0;
            var result = (await _wishlistItemDA.GetWishlistItems(internalUserId)).Select(x => 
            {
                var obj = new ExpandoObject();
                var item = obj as IDictionary<string, object>;
                if (includeAll || fieldList.Contains("appid"))
                    item["appid"] = x.appid;
                if(includeAll || fieldList.Contains("dateadded"))
                    item["dateadded"] = x.DateAdded;
                if (x.AppListing != null && (includeAll || fieldList.Contains("name")))
                    item["name"] = x.AppListing.name;
                return obj;
            });
            return Ok(result);
        }

        [HttpPost("{appId}")]
        public async Task<ActionResult> AddWishlistItem(int appId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserId(new Guid(userId));
            await _wishlistItemDA.AddWishlistItem(new WishlistItem(){
                UserID = internalUserId, 
                appid = appId
            });

            return Ok();
        }

        [HttpDelete("{appId}")]
        public async Task<ActionResult> DeleteAppFromWishlist(int appId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserId(new Guid(userId));
            await _wishlistItemDA.DeleteWishlistItem(internalUserId, appId);

            return Ok();
        }
    }
}
