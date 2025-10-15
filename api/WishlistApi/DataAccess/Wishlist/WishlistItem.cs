using DataAccess.AppListings;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
