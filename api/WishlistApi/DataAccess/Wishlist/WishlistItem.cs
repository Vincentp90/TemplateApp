using DataAccess.AppListings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Wishlist
{
    public class WishlistItem
    {
        public int ID { get; set; }

        //[ForeignKey("User")]
        public string UserID { get; set; }
        public DateTimeOffset DateAdded { get; set; }

        [ForeignKey("AppListing")]
        public int appid { get; set; }
        public AppListing? AppListing { get; set; }

    }
}
