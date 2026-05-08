using Application;
using DataAccess.AppListings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AppListingsController : ControllerBase
    {
        private readonly IAppListingService _appListingService;

        public AppListingsController(IAppListingService appListingService)
        {
            _appListingService = appListingService;
        }

        [HttpGet("search/{term}")]
        public async Task<ActionResult> SearchAsync(string term)
        {
            return Ok(await _appListingService.SearchAppListingsAsync(term));
        }
    }
}
