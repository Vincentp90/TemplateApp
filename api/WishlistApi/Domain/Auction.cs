using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain
{
    public class Auction
    {
        public int Id { get; set; }
        public decimal? CurrentPrice { get; set; }
        public decimal StartingPrice { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public AuctionStatus Status { get; set; }
        public int? UserId { get; set; }
        
        public void PlaceBid(int userId, decimal amount)
        {
            if (amount <= StartingPrice)
                throw new DomainException("Bid must be higher than starting price.");

            if (amount <= CurrentPrice)
                throw new DomainException("Bid must be higher than current price.");

            CurrentPrice = amount;
            UserId = userId;
        }

        //TODO this probably should be different with aggregate root
        public int appid { get; set; }
        public AppListing AppListing { get; set; }
        public Guid? UserUUID { get; set; }
        // To be removed once we stop doing client side OCC
        public required uint RowVersion { get; set; }
    }

    // Temporary until we learned how aggregate root works
    public class AppListing
    {
        public int appid { get; set; }
        public required string name { get; set; }

    }  

    public enum AuctionStatus
    {
        Open,
        Closed
    }
}
