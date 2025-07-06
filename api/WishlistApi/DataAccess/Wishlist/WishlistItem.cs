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

        [ForeignKey("AppListing")]
        public string appid { get; set; }


    }
}
