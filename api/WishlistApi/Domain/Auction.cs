using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class Auction
    {
        public decimal? CurrentPrice { get; set; }
        public decimal StartingPrice { get; set; }
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
    }
}
