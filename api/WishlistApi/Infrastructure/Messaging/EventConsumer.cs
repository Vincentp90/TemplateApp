using Application.Events;
using MassTransit;

namespace Infrastructure.Messaging;

/// <summary>
/// MassTransit consumer for wishlist events.
/// Currently a placeholder — the actual consumer lives in SteamTracker.
/// Registered here so MassTransit knows about the event types and can
/// declare the proper exchanges and bindings on the RabbitMQ side.
/// </summary>
public class EventConsumer : IConsumer<WishlistItemAdded>, IConsumer<WishlistItemRemoved>
{
    public Task Consume(ConsumeContext<WishlistItemAdded> context)
    {
        // Intentionally empty — the real consumer is in SteamTracker.
        // This consumer exists so MassTransit declares the exchange and bindings.
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<WishlistItemRemoved> context)
    {
        // Intentionally empty — the real consumer is in SteamTracker.
        // This consumer exists so MassTransit declares the exchange and bindings.
        return Task.CompletedTask;
    }
}
