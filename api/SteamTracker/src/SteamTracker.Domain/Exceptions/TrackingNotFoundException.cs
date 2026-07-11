namespace SteamTracker.Domain.Exceptions;

/// <summary>
/// Thrown when a tracked game is not found or is inactive.
/// </summary>
public class TrackingNotFoundException : Exception
{
    public TrackingNotFoundException(string message) : base(message) { }

    public TrackingNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
