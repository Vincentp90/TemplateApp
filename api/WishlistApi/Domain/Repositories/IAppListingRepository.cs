namespace Domain.Repositories;

public interface IAppListingRepository
{
    Task<List<AppListing>> SearchAsync(string term);
    Task<AppListing> GetRandomAsync();
    Task<bool> HasAnyAsync();
    Task SaveAsync(IEnumerable<AppListing> listings);
}
