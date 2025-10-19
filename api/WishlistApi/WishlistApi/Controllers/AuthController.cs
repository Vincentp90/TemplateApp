using DataAccess.AppListings;
using Microsoft.AspNetCore.Mvc;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly AppListingDA _appListingDA;

        public AuthController(ILogger<AuthController> logger, AppListingDA appListingDA)
        {
            _logger = logger;
            _appListingDA = appListingDA;
        }

        [HttpPost("register")]
        public ActionResult Register(string todo)
        {
            //TODO
            return Ok();
        }

        [HttpPost("login")]
        public ActionResult Login(string todo)
        {
            //TODO
            return Ok();
        }

        // TODO what is the REST way to do this
        [HttpGet("check")]
        public ActionResult CheckUsernameAvailable(string todo)
        {
            //TODO
            return Ok();
        }
    }
}
