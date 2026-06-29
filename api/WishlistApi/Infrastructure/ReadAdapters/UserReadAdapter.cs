using Application.Contracts;
using Application.Queries;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ReadAdapters;

public class UserReadAdapter(WishlistDbContext context) : IUserReadModel
{
    public async Task<List<UserSummaryDto>> GetUsersAsync(int page, int limit)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 200);
        return await context.Users
            .OrderBy(u => u.Username)
            .Skip((page - 1) * limit)
            .Take(limit + 1)
            .Select(u => new UserSummaryDto(u.UUID, u.Username))
            .ToListAsync();
    }
}
