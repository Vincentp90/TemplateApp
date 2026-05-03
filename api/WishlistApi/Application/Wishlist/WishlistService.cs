using Application.Domain;
using DataAccess.Wishlist;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Wishlist
{
    public interface IWishlistService
    {
        WishlistStats GetWishlistStats(int userID);
    }

    public class WishlistService
    {
        //private readonly IWishlistRepository _wishlistRepository;TODO
        private readonly IWishlistItemDA _wishlistItemDA;

        public WishlistService(IWishlistItemDA wishlistItemDA)
        {
            _wishlistItemDA = wishlistItemDA;
        }

        public WishlistStats GetWishlistStats(int userID)
        {
            throw new NotImplementedException();
            return new WishlistStats();
        }
    }
}
