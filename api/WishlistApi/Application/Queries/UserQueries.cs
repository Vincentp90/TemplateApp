using Application.Contracts;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries;

public interface IUserQueries
{
    Task<List<UserSummaryDto>> GetUsersAsync(int page, int limit);
}

public class UserQueries(WishlistDbContext context) : IUserQueries
{
    public async Task<List<UserSummaryDto>> GetUsersAsync(int page, int limit)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 200);
        return await context.Users
            .OrderBy(u => u.Username)
            .Skip((page-1) * limit)
            .Take(limit + 1)
            .Select(u => new UserSummaryDto(u.UUID, u.Username))
            .ToListAsync();
    }
}