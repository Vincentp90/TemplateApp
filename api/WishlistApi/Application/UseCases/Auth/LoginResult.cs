namespace Application.UseCases.Auth;

/// <summary>
/// Result of a successful login operation.
/// </summary>
public record LoginResult(Guid UserId, string Username, string Role);
