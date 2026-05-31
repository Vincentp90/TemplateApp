// TODO DELETE THIS
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
        Task<AppListing> GetRandomAppListingAsync();
        Task<bool> HasAnyAsync();
        Task SaveAppListingsAsync(IEnumerable<AppListing> listings);
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
            return await _context.AppListings
                .FromSqlRaw("SELECT * FROM app_listings WHERE name % {0} ORDER BY similarity(name, {0}) DESC", term)
                .ToListAsync();
        }

        public async Task<AppListing> GetRandomAppListingAsync()
        {
            var count = await _context.AppListings.CountAsync();
            var index = Random.Shared.Next(count);
            return await _context.AppListings
                .OrderBy(x => x.appid)
                .Skip(index)
                .FirstAsync();
        }

        public async Task<bool> HasAnyAsync()
        {
            return await _context.AppListings.AnyAsync();
        }

        public async Task SaveAppListingsAsync(IEnumerable<AppListing> listings)
        {
            _context.AppListings.AddRange(listings);
            await _context.SaveChangesAsync();
        }
    }
}
