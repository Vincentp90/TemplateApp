using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Repositories
{
    public interface IAuctionRepository
    {
        Task<Auction?> GetOpenAuction(int id);
        void Update(Auction auction, uint rowVersion);


        Task<Auction?> GetLatestAuctionAsync();
        void AddAuction(Auction auction);
        Task CloseAuctionAndAddNewAsync(Auction newAuction);
    }
}
