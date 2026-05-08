using Application;
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
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserDTOs.UserDetails>> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            return await GetUserDetailsDTO(userId);
        }

        [HttpGet("{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDTOs.UserDetails>> Index([FromRoute] string UserId)
        {
            return await GetUserDetailsDTO(UserId);
        }

        private async Task<ActionResult<UserDTOs.UserDetails>> GetUserDetailsDTO(string UserId)
        {
            int internalUserId = await _userService.GetInternalUserIdAsync(new Guid(UserId));

            var userWithDetails = await _userService.GetUserDetailsAsync(internalUserId);
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

        [HttpPatch("me")]
        public async Task<ActionResult> PatchUserAsync(UserDTOs.UserDetails userDetailsDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");
            return await UpdateUserDetails(userDetailsDTO, userId);
        }

        [HttpPatch("{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDTOs.UserDetails>> PatchUserAsync(UserDTOs.UserDetails userDetailsDTO, [FromRoute] string UserId)
        {
            return await UpdateUserDetails(userDetailsDTO, UserId);
        }

        private async Task<ActionResult> UpdateUserDetails(UserDTOs.UserDetails userDetailsDTO, string userId)
        {
            int internalUserId = await _userService.GetInternalUserIdAsync(new Guid(userId));
            var userDetailsEntity = await _userService.GetUserDetailsAsync(internalUserId);
            try
            {
                userDetailsEntity.RowVersion = userDetailsDTO.RowVersion;

                userDetailsEntity.FirstName = userDetailsDTO.FirstName;
                userDetailsEntity.LastName = userDetailsDTO.LastName;
                userDetailsEntity.Country = userDetailsDTO.Country;
                userDetailsEntity.City = userDetailsDTO.City;
                userDetailsEntity.Address = userDetailsDTO.Address;

                await _userService.UpdateUserDetailsAsync(userDetailsEntity);
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }
        }

        [HttpGet("")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAllUsersAsync(int page, int limit)
        {
            var users = await _userService.GetUsersAsync(page, limit);
            var hasNextPage = users.Count > limit;
            var usersDTO = users.Take(limit).Select(u => new
            {
                uuid = u.UUID,
                username = u.Username
            }).ToList();
            return Ok(new
            {
                items = usersDTO,
                hasNextPage = hasNextPage
            });
        }
    }
}
