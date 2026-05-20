using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain
{
    public class Auction
    {
        public int Id { get; private set; }
        public decimal? CurrentPrice { get; internal set; }
        public decimal StartingPrice { get; internal set; }
        public DateTimeOffset DateAdded { get; internal set; }
        public AuctionStatus Status { get; internal set; }
        public int? UserId { get; private set; }
        public int AppListingId { get; internal set; }
        // To be removed once we stop doing client side OCC
        public uint RowVersion { get; internal set; }

        // Default constructor for creating new auctions (used by Application layer)
        public Auction()
        {
        }

        // Constructor for mapping from Data Access layer (GetLatestAuctionAsync) and Application layer
        public Auction(
            int id,
            DateTimeOffset dateAdded,
            decimal? currentPrice,
            decimal startingPrice,
            AuctionStatus status,
            int? userId,
            int appListingId,
            uint rowVersion)
        {
            Id = id;
            DateAdded = dateAdded;
            CurrentPrice = currentPrice;
            StartingPrice = startingPrice;
            Status = status;
            UserId = userId;
            AppListingId = appListingId;
            RowVersion = rowVersion;
        }

        public void PlaceBid(int bidderUserId, decimal amount)
        {
            if (amount <= StartingPrice)
                throw new DomainException("Bid must be higher than starting price.");

            if (amount <= CurrentPrice)
                throw new DomainException("Bid must be higher than current price.");

            CurrentPrice = amount;
            UserId = bidderUserId;
        }
    }

    public enum AuctionStatus
    {
        Open,
        Closed
    }
}
