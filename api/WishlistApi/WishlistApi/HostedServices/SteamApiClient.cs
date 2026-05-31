using Domain;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace WishlistApi.HostedServices
{
    public class SteamApiClient : ISteamApiClient
    {
        private const string _steamApiUrl = "https://api.steampowered.com/IStoreService/GetAppList/v1/";

        public async Task<SteamAppList?> GetAppListingsAsync(string apiKey)
        {
            using HttpClient client = new HttpClient();
            var url = QueryHelpers.AddQueryString(_steamApiUrl, new Dictionary<string, string?>
            {
                ["key"] = apiKey,
                ["max_results"] = "30000",
                ["include_dlc"] = "false",
            });
            var response = await client.GetStringAsync(url);
            var deserialized = JsonSerializer.Deserialize<Root>(response);
            if (deserialized == null || deserialized.response == null)
                return null;

            var apps = deserialized.response.apps
                .Select(a => new SteamAppEntry(a.appid, a.name))
                .ToList();

            return new SteamAppList(apps);
        }
    }

    internal class Root
    {
        public required AppList response { get; set; }
    }

    internal class AppList
    {
        public required List<AppEntry> apps { get; set; }
    }

    internal class AppEntry
    {
        public int appid { get; set; }
        public required string name { get; set; }
    }
}
