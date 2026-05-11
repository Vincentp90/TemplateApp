using Application;
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
        private readonly IWishlistService _wishlistService;        

        public WishlistController(IUserContext userContext, IWishlistService wishlistService)
        {
            _userContext = userContext;
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
            
            bool Has(string field) => includeAll || fieldList.Contains(field);

            var result = (await _wishlistService.GetWishlistItemsAsync(internalUserId))
                .Select(x => new WishlistDTOs.WishlistItemDto(
                    AppId: Has("appid") ? x.appid : null,
                    DateAdded: Has("dateadded") ? x.DateAdded : null,
                    Name: Has("name") ? x.AppListing?.name : null
                ));

            return Ok(new WishlistDTOs.Wishlist(result));
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
                await _wishlistService.AddWishlistItemAsync(new WishlistItem()
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
            await _wishlistService.DeleteWishlistItemAsync(internalUserId, appId);
            return Ok();
        }
    }
}
