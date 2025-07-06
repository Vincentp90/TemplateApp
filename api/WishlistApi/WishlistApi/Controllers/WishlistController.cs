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

        [HttpGet("wishlist")]
        public ActionResult GetWishlist()
        {
            string? userId = Request.Headers["x-user-id"];
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            return Ok(_wishlistItemDA.GetWishlistItems(userId));
        }

        [HttpPost("wishlist/{appId}")]
        public ActionResult AddWishlistItem(string appId)
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

        [HttpDelete("wishlist/{appId}")]
        public ActionResult DeleteAppFromWishlist(string appId)
        {
            string? userId = Request.Headers["x-user-id"];
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Missing x-user-id header");

            _wishlistItemDA.DeleteWishlistItem(userId, appId);

            return Ok();
        }

    }
}
