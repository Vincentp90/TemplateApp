using DataAccess.AppListings;
using DataAccess.Wishlist;
using Microsoft.AspNetCore.Mvc;
using WishlistApi.Steam;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly WishlistItemDA _wishlistItemDA;

        public WishlistController(WishlistItemDA wishlistItemDA)
        {
            _wishlistItemDA = wishlistItemDA;
        }

        [HttpGet()]
        public ActionResult GetWishlist([FromHeader(Name = "x-user-id")] string userId)
        {
            //string? userId = Request.Headers["x-user-id"];
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            return Ok(_wishlistItemDA.GetWishlistItems(userId).Select(x => new { appid = x.appid, name = x.AppListing?.name }));
        }

        [HttpPost("{appId}")]
        public ActionResult AddWishlistItem(int appId)
        {
            //TODO get user id from x-user-id header, real auth later
            string? userId = Request.Headers["x-user-id"];
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            _wishlistItemDA.AddWishlistItem(new WishlistItem(){
                userid = userId, 
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
