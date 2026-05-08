using DataAccess.AppListings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IAppListingService
    {
        Task<List<AppListing>> SearchAppListingsAsync(string term);
        Task<List<AppListing>> GetAppListingsAsync();
        Task<AppListing> GetRandomAppListingAsync();
    }

    public class AppListingService : IAppListingService
    {
        private readonly IAppListingDA _appListingDA;

        public AppListingService(IAppListingDA appListingDA)
        {
            _appListingDA = appListingDA;
        }

        public async Task<List<AppListing>> GetAppListingsAsync()
        {
            return await _appListingDA.GetAppListingsAsync();
        }

        public async Task<AppListing> GetRandomAppListingAsync()
        {
            return await _appListingDA.GetRandomAppListingAsync();
        }

        public async Task<List<AppListing>> SearchAppListingsAsync(string term)
        {
            return await _appListingDA.SearchAppListingsAsync(term);
        }
    }
}
