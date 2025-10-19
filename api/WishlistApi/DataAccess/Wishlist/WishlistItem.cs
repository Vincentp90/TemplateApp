using DataAccess.AppListings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Wishlist
{
    public class WishlistItem
    {
        public int id { get; set; }

        //[ForeignKey("User")]
        public string userid { get; set; }
        public DateTimeOffset dateadded { get; set; }

        [ForeignKey("AppListing")]
        public int appid { get; set; }
        public AppListing? AppListing { get; set; }

    }
}
