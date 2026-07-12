namespace Application.UseCases.Auth.Requests;

/// <summary>
/// Request for registering a new user.
/// </summary>
public record RegisterUserRequest(string Username, string Password);
