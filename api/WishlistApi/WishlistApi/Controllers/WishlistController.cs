using DataAccess.AppListings;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly WishlistItemDA _wishlistItemDA;

        public WishlistController(WishlistItemDA wishlistItemDA)
        {
            _wishlistItemDA = wishlistItemDA;
        }

        [HttpGet()]
        public ActionResult GetWishlist([FromHeader(Name = "x-user-id")] string userId, [FromQuery] string? fields = null)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            var fieldList = (fields ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(f => f.ToLower())
                                .ToHashSet();
            bool includeAll = fieldList.Count == 0;
            var result = _wishlistItemDA.GetWishlistItems(userId).Select(x => 
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
        public ActionResult AddWishlistItem(int appId)
        {
            //TODO get user id from x-user-id header, real auth later
            string? userId = Request.Headers["x-user-id"];
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            _wishlistItemDA.AddWishlistItem(new WishlistItem(){
                UserID = userId, 
                appid = appId
            });

            return Ok();
        }

        [HttpDelete("{appId}")]
        public ActionResult DeleteAppFromWishlist(int appId)
        {
            string? userId = Request.Headers["x-user-id"];
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            _wishlistItemDA.DeleteWishlistItem(userId, appId);

            return Ok();
        }

    }
}
