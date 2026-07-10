using SteamTracker.Infrastructure.External;
using System.IO;

namespace SteamTracker.Worker;

internal static class WorkerHelpers
{
    /// <summary>
    /// Classifies an exception as transient (retryable) or programming error (dead-letter).
    /// </summary>
    public static bool IsTransient(Exception ex)
    {
        return ex is
            TimeoutException or
            OperationCanceledException or
            HttpRequestException or
            SteamRateLimitException or
            IOException;
    }
}
