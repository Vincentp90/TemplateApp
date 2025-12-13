using DataAccess.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Net;
using System.Security.Claims;
using WishlistApi.DTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserDA _userDA;

        public UserController(IUserDA userDA)
        {
            _userDA = userDA;
        }

        [HttpGet()]
        public async Task<ActionResult<UserDTOs.UserDetails>> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));

            var userWithDetails = await _userDA.GetUserDetails(internalUserId);
            return Ok(new UserDTOs.UserDetails(
                RowVersion: userWithDetails.RowVersion,
                Email: userWithDetails.User.Username,
                FirstName: userWithDetails.FirstName,
                LastName: userWithDetails.LastName,
                Country: userWithDetails.Country,
                City: userWithDetails.City,
                Address: userWithDetails.Address
            ));
        }
    }
}
