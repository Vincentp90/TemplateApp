namespace SteamTracker.Domain.Exceptions;

/// <summary>
/// Thrown when an alert rule is not found or the user is not authorized.
/// </summary>
public class AlertRuleNotFoundException : Exception
{
    public AlertRuleNotFoundException(string message) : base(message) { }

    public AlertRuleNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
