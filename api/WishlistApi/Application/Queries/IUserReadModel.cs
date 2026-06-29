namespace Application.Queries;

public interface IUserReadModel
{
    Task<List<Contracts.UserSummaryDto>> GetUsersAsync(int page, int limit);
}
