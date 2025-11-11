using DataAccess.AppListings;
using DataAccess.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Auctions
{
    public class Auction
    {
        public static TimeSpan Duration = TimeSpan.FromMinutes(30);

        [Key]
        public int ID { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public AuctionStatus Status { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal? CurrentPrice { get; set; }

        [Timestamp]
        public uint RowVersion { get; set; }

        [ForeignKey("AppListing")]
        public int appid { get; set; }
        public AppListing AppListing { get; set; }

        [ForeignKey("User")]
        public int? UserID { get; set; }
        public User? User { get; set; }
    }

    public enum AuctionStatus
    {
        Open,
        Closed
    }
}
