namespace SteamTracker.Infrastructure.External;

public class SteamRateLimitException : Exception
{
    public SteamRateLimitException(string message) : base(message) { }
}
