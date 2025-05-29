using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AppListings
{
    public class AppListing
    {
        [Key]
        public int appid { get; set; }
        public string name { get; set; }

    }
}
