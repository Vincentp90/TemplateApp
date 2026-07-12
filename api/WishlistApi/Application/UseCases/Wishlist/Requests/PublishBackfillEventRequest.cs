namespace Application.UseCases.Wishlist.Requests;

/// <summary>
/// Request for publishing a backfill event for a wishlist item.
/// </summary>
public record PublishBackfillEventRequest(int UserId, int AppId, System.DateTimeOffset DateAdded);
