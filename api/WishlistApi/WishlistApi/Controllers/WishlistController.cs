using Application.Wishlist;
using DataAccess.AppListings;
using DataAccess.Users;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Dynamic;
using System.Security.Claims;
using WishlistApi.DTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistItemDA _wishlistItemDA;
        private readonly IUserDA _userDA;
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistItemDA wishlistItemDA, IUserDA userDA, IWishlistService wishlistService)
        {
            _wishlistItemDA = wishlistItemDA;
            _userDA = userDA;
            _wishlistService = wishlistService;
        }

        [HttpGet()]
        public async Task<ActionResult<WishlistDTOs.Wishlist>> GetWishlistAsync([FromQuery] string? fields = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));

            var fieldList = (fields ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(f => f.ToLower())
                                .ToHashSet();
            bool includeAll = fieldList.Count == 0;
            
            var result = (await _wishlistItemDA.GetWishlistItemsAsync(internalUserId)).Select(x => 
            {
                //TODO better way to do this? DTO and leave fields empty when not included?
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
            return Ok(new WishlistDTOs.Wishlist( Items: result ));
        }

        [HttpGet("stats")]
        public async Task<ActionResult<WishlistDTOs.Stats>> GetWishlistStatsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));

            var stats = await _wishlistService.GetWishlistStatsAsync(internalUserId);
            return Ok(new WishlistDTOs.Stats(
                AvgTimeAdded: stats.AvgTimeAdded,
                AvgTimeBetweenAdded: stats.AvgTimeBetweenAdded,
                OldestItem: stats.OldestItem,
                MostCommonCharacter: stats.MostCommonCharacter
                ));
        }

        [HttpPost("{appId}")]
        public async Task<ActionResult> AddWishlistItemAsync(int appId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));
            try
            {
                await _wishlistItemDA.AddWishlistItemAsync(new WishlistItem()
                {
                    UserID = internalUserId,
                    appid = appId
                });
            }
            catch (DuplicateNameException ex)
            {
                return StatusCode(StatusCodes.Status409Conflict, ex.Message);
            }

            return Ok();
        }

        [HttpDelete("{appId}")]
        public async Task<ActionResult> DeleteAppFromWishlistAsync(int appId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));
            await _wishlistItemDA.DeleteWishlistItemAsync(internalUserId, appId);

            return Ok();
        }
    }
}
