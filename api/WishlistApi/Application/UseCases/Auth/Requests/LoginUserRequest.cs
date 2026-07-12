namespace Application.UseCases.Auth.Requests;

/// <summary>
/// Request for logging in.
/// </summary>
public record LoginUserRequest(string Username, string Password);
