using DataAccess.AppListings;
using Microsoft.AspNetCore.Mvc;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppListingController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly AppListingDA _appListingDA;

        public AppListingController(ILogger<WeatherForecastController> logger, AppListingDA appListingDA)
        {
            _logger = logger;
            _appListingDA = appListingDA;
        }

        [HttpGet("search/{term}")]
        public ActionResult Search(string term)
        {
            return Ok(_appListingDA.SearchAppListings(term).Select(a => a.name));
        }
    }
}
