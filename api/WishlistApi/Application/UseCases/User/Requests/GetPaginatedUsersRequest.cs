namespace Application.UseCases.User.Requests;

/// <summary>
/// Request for retrieving a page of users.
/// </summary>
public record GetPaginatedUsersRequest(int Page, int Limit);
