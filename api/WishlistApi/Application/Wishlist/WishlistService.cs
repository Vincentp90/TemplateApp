using Application.Domain;
using DataAccess.Wishlist;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Wishlist
{
    public interface IWishlistService
    {
        Task<WishlistStats> GetWishlistStats(int userID);
    }

    public class WishlistService
    {
        //private readonly IWishlistRepository _wishlistRepository;TODO
        private readonly IWishlistItemDA _wishlistItemDA;

        public WishlistService(IWishlistItemDA wishlistItemDA)
        {
            _wishlistItemDA = wishlistItemDA;
        }

        public async Task<WishlistStats> GetWishlistStats(int userID)
        {
            var items = await _wishlistItemDA.GetWishlistItemsAsync(userID);
            throw new NotImplementedException();
            return new WishlistStats();
        }
    }
}
