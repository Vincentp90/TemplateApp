using Application;
using Application.Contracts;
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
        public async Task<ActionResult<List<AppListingDto>>> SearchAsync(string term)
        {
            return Ok(await _appListingService.SearchAppListingsAsync(term));
        }
    }
}
