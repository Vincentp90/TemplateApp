namespace Domain
{
    public record SteamAppEntry(int AppId, string Name);

    public record SteamAppList(IList<SteamAppEntry> Apps);

    public interface ISteamApiClient
    {
        Task<SteamAppList?> GetAppListingsAsync(string apiKey);
    }
}
