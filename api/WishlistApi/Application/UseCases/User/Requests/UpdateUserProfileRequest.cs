using Domain.ValueObjects;

namespace Application.UseCases.User.Requests;

/// <summary>
/// Request for updating a user profile.
/// </summary>
public record UpdateUserProfileRequest(
    Guid ExternalUserId,
    uint RowVersion,
    FullName Name,
    Address Location);
