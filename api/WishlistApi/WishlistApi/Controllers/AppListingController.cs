using DataAccess.AppListings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class AppListingController : ControllerBase
    {
        private readonly ILogger<AppListingController> _logger;
        private readonly IAppListingDA _appListingDA;

        public AppListingController(ILogger<AppListingController> logger, IAppListingDA appListingDA)
        {
            _logger = logger;
            _appListingDA = appListingDA;
        }

        [HttpGet("search/{term}")]
        public async Task<ActionResult> SearchAsync(string term)
        {
            return Ok(await _appListingDA.SearchAppListingsAsync(term));
        }
    }
}
