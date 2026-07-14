namespace SteamTracker.API.Models;

/// <summary>
/// DTO for returning game price information from the SteamTracker API.
/// </summary>
public record GamePriceDto(int AppId, decimal? Amount, string Currency, DateTimeOffset? LastCheckedAt, bool IsUnavailable);
