using DataAccess.AppListings;
using DataAccess.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Wishlist
{
    public class WishlistItem
    {
        public int ID { get; set; }
        public DateTimeOffset DateAdded { get; set; }

        [ForeignKey("AppListing")]
        public int appid { get; set; }
        public AppListing? AppListing { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public User? User { get; set; }
    }
}
