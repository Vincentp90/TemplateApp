using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace WishlistApi.HostedServices
{
    public interface ISteamApiClient
    {
        Task<Root?> GetAppListingsAsync(string apiKey);
    }

    public class SteamApiClient : ISteamApiClient
    {
        private const string _steamApiUrl = "https://api.steampowered.com/IStoreService/GetAppList/v1/";

        public async Task<Root?> GetAppListingsAsync(string apiKey)
        {
            using HttpClient client = new HttpClient();
            var url = QueryHelpers.AddQueryString(_steamApiUrl, new Dictionary<string, string?>
            {
                ["key"] = apiKey,
                ["max_results"] = "30000",
                ["include_dlc"] = "false",
            });
            var response = await client.GetStringAsync(url);
            var appListings = JsonSerializer.Deserialize<Root>(response);
            return appListings;
        }
    }

    public class Root
    {
        public required AppList response { get; set; }
    }

    public class AppList
    {
        public required List<DataAccess.AppListings.AppListing> apps { get; set; }
    }
}
