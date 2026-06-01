using Infrastructure.Persistence.AppListings;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.AppListings;

public class AppListingRepository(WishlistDbContext context) : IAppListingRepository
{
    public async Task<List<Domain.AppListing>> SearchAsync(string term)
    {
        var entities = await context.AppListings
            .FromSqlRaw("SELECT * FROM app_listings WHERE name % {0} ORDER BY similarity(name, {0}) DESC", term)
            .ToListAsync();

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<Domain.AppListing> GetRandomAsync()
    {
        var count = await context.AppListings.CountAsync();
        var index = Random.Shared.Next(count);
        var entity = await context.AppListings
            .OrderBy(x => x.appid)
            .Skip(index)
            .FirstAsync();

        return MapToDomain(entity);
    }

    public async Task<bool> HasAnyAsync()
    {
        return await context.AppListings.AnyAsync();
    }

    public async Task SaveAsync(IEnumerable<Domain.AppListing> listings)
    {
        var entities = listings.Select(MapToEntity);
        context.AppListings.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private static Domain.AppListing MapToDomain(Infrastructure.Persistence.AppListings.AppListing entity)
    {
        return new Domain.AppListing(entity.appid, entity.name);
    }

    private static Infrastructure.Persistence.AppListings.AppListing MapToEntity(Domain.AppListing domain)
    {
        return new Infrastructure.Persistence.AppListings.AppListing
        {
            appid = domain.Id,
            name = domain.Name
        };
    }
}
