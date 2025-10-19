using System.ComponentModel.DataAnnotations;

namespace DataAccess.AppListings
{
    public class AppListing
    {
        [Key]
        public int appid { get; set; }
        public string name { get; set; }

    }
}
