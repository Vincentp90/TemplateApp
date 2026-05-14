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
        public int AppListingId { get; set; }

        public void PlaceBid(int userId, decimal amount)
        {
            if (amount <= StartingPrice)
                throw new DomainException("Bid must be higher than starting price.");

            if (amount <= CurrentPrice)
                throw new DomainException("Bid must be higher than current price.");

            CurrentPrice = amount;
            UserId = userId;
        }

        // To be removed once we stop doing client side OCC
        public required uint RowVersion { get; set; }
    }

    public enum AuctionStatus
    {
        Open,
        Closed
    }
}
