using Application.Contracts;
using Application.UseCases.AppListing;
using Application.UseCases.AppListing.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AppListingsController(ISearchAppListingsUseCase searchAppListingsUseCase) : ControllerBase
    {
        [HttpGet("search/{term}")]
        public async Task<ActionResult<List<AppListingDto>>> SearchAsync(string term)
        {
            return Ok(await searchAppListingsUseCase.ExecuteAsync(new SearchAppListingsRequest(term)));
        }
    }
}
