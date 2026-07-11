using Application.Contracts;
using Application.UseCases.User.Requests;
using Application.Queries;

namespace Application.UseCases.User;

/// <summary>
/// Use case: retrieve a page of users (admin endpoint).
/// </summary>
public class GetPaginatedUsersUseCase(IUserReadModel userReadModel) : IGetPaginatedUsersUseCase
{
    public async Task<IReadOnlyList<UserSummaryDto>> ExecuteAsync(GetPaginatedUsersRequest request)
    {
        return await userReadModel.GetUsersAsync(request.Page, request.Limit);
    }
}
