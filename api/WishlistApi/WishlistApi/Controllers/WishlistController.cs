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
using WishlistApi.Helpers;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IUserContext _userContext;
        private readonly IWishlistItemDA _wishlistItemDA;
        private readonly IWishlistService _wishlistService;        

        public WishlistController(IUserContext userContext, IWishlistItemDA wishlistItemDA, IWishlistService wishlistService)
        {
            _userContext = userContext;
            _wishlistItemDA = wishlistItemDA;
            _wishlistService = wishlistService;            
        }

        [HttpGet()]
        public async Task<ActionResult<WishlistDTOs.Wishlist>> GetWishlistAsync([FromQuery] string? fields = null)
        {
            int internalUserId = await _userContext.GetIdAsync();

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
            int internalUserId = await _userContext.GetIdAsync();

            var stats = await _wishlistService.GetWishlistStatsAsync(internalUserId);
            return Ok(new WishlistDTOs.Stats(
                AvgTimeAdded: stats.AvgTimeAdded,
                AvgTimeBetweenAdded: stats.AvgTimeBetweenAdded,
                OldestItem: stats.OldestItem,
                MostCommonCharacter: stats.MostCommonCharacter
                ));
        }

        // TODO route doesn't make that much sense, /wishlist/apps/{appId} would be better
        [HttpPost("{appId}")]
        public async Task<ActionResult> AddWishlistItemAsync(int appId)
        {
            int internalUserId = await _userContext.GetIdAsync();
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

        // TODO route, see above
        [HttpDelete("{appId}")]
        public async Task<ActionResult> DeleteAppFromWishlistAsync(int appId)
        {
            int internalUserId = await _userContext.GetIdAsync();
            await _wishlistItemDA.DeleteWishlistItemAsync(internalUserId, appId);
            return Ok();
        }
    }
}
