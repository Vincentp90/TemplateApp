using Application.Contracts;
using Application.UseCases.AppListing.Requests;
using Domain.Repositories;

namespace Application.UseCases.AppListing;

/// <summary>
/// Use case: search app listings by term.
/// Returns empty list if term is too short.
/// </summary>
public class SearchAppListingsUseCase(IAppListingRepository appListingRepository) : ISearchAppListingsUseCase
{
    public async Task<IReadOnlyList<AppListingDto>> ExecuteAsync(SearchAppListingsRequest request)
    {
        if (string.IsNullOrEmpty(request.Term) || request.Term.Length < 3)
            return Array.Empty<AppListingDto>();

        var results = await appListingRepository.SearchAsync(request.Term);
        return results.Select(a => new AppListingDto(a.Id, a.Name)).ToList();
    }
}
