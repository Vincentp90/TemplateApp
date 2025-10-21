using DataAccess.AppListings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Wishlist
{
    public class WishlistItem
    {
        public int ID { get; set; }

        //TODO FK to User ID, also in DA get user ID first instead of using User.UUID for this field -> this might be could example for caching
        //[ForeignKey("User")]
        public string UserID { get; set; } // TODO index 
        public DateTimeOffset DateAdded { get; set; }

        [ForeignKey("AppListing")]
        public int appid { get; set; }
        public AppListing? AppListing { get; set; }

    }
}
