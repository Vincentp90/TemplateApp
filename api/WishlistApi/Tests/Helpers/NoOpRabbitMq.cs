using Application;

namespace Tests.Helpers;

/// <summary>
/// No-op event publisher — silently discards all events.
/// Used in integration tests so the app doesn't need a real message broker.
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync(object @event)
    {
        // Silently discard — events are not published in test mode
        return Task.CompletedTask;
    }
}
