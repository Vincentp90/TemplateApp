namespace Application.UseCases.User.Requests;

/// <summary>
/// Request for retrieving a user profile.
/// </summary>
public record GetUserProfileRequest(Guid ExternalUserId);
