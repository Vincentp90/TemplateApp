using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AppListings
{
    public interface IAppListingDA
    {
        Task<List<AppListing>> SearchAppListingsAsync(string term);
        Task<List<AppListing>> GetAppListingsAsync();
        Task<AppListing> GetRandomAppListingAsync();
    }

    public class AppListingDA : IAppListingDA
    {
        private readonly WishlistDbContext _context;

        public AppListingDA(WishlistDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<List<AppListing>> SearchAppListingsAsync(string term)
        {
            if(string.IsNullOrEmpty(term) || term.Length < 3)
                return new List<AppListing>();
            return await _context.AppListings
                .FromSqlRaw("SELECT * FROM app_listings WHERE similarity(name, {0}) > 0.3 ORDER BY similarity(name, {0}) DESC", term)
                .ToListAsync();
        }

        public async Task<AppListing> GetRandomAppListingAsync()
        {
            // TODO look into more optimal ways to do this
            return await _context.AppListings
                .OrderBy(x => Guid.NewGuid())
                .FirstAsync();
        }

        public async Task<List<AppListing>> GetAppListingsAsync()
        {
            return await _context.AppListings.Take(100).ToListAsync();
        }


    }

}
