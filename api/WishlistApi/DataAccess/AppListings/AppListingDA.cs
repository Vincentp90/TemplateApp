using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AppListings
{
    public class AppListingDA
    {
        private readonly WishlistDbContext _context;

        public AppListingDA(WishlistDbContext dbContext)
        {
            _context = dbContext;
        }

        public List<AppListing> SearchAppListings(string term)
        {
            if(string.IsNullOrEmpty(term) || term.Length < 3)
                return new List<AppListing>();
            return _context.AppListings
                .FromSqlRaw("SELECT * FROM app_listings WHERE similarity(your_column, {0}) > 0.3 ORDER BY similarity(your_column, {0}) DESC", term)
                .ToList();
        }

        public List<AppListing> GetAppListings()
        {
            return _context.AppListings.Take(100).ToList();
        }


    }

}
