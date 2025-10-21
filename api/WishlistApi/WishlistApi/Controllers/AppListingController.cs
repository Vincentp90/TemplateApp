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
        private readonly AppListingDA _appListingDA;

        public AppListingController(ILogger<AppListingController> logger, AppListingDA appListingDA)
        {
            _logger = logger;
            _appListingDA = appListingDA;
        }

        [HttpGet("search/{term}")]
        public async Task<ActionResult> Search(string term)
        {
            return Ok(await _appListingDA.SearchAppListings(term));
        }
    }
}
