using DataAccess.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Net;
using System.Security.Claims;
using WishlistApi.DTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserDA _userDA;

        public UsersController(IUserDA userDA)
        {
            _userDA = userDA;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserDTOs.UserDetails>> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));

            var userWithDetails = await _userDA.GetUserDetailsAsync(internalUserId);
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

        [HttpPost("me")]
        public async Task<ActionResult> PostUserAsync(UserDTOs.UserDetails userDetailsDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            int internalUserId = await _userDA.GetInternalUserIdAsync(new Guid(userId));
            var userDetailsEntity = await _userDA.GetUserDetailsAsync(internalUserId);
            try
            {
                userDetailsEntity.RowVersion = userDetailsDTO.RowVersion;

                userDetailsEntity.FirstName = userDetailsDTO.FirstName;
                userDetailsEntity.LastName = userDetailsDTO.LastName;
                userDetailsEntity.Country = userDetailsDTO.Country;
                userDetailsEntity.City = userDetailsDTO.City;
                userDetailsEntity.Address = userDetailsDTO.Address;

                await _userDA.UpdateUserDetailsAsync(userDetailsEntity);
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }

        [HttpGet("")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAllUsersAsync()
        {
            var users = await _userDA.GetUsersAsync();
            return Ok(users.Select(u => new {                
                uuid = u.UUID,
                username = u.Username
            }).ToList());
        }
    }
}
