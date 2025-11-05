using System.ComponentModel.DataAnnotations;

namespace DataAccess.AppListings
{
    public class AppListing
    {
        // Using lowercase so we can directly deserialize from steamapi into this object
        [Key]
        public int appid { get; set; }
        public required string name { get; set; }

    }
}
