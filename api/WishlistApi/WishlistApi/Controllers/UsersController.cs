using Application;
using Application.Commands;
using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WishlistApi.DTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UsersController(IUserService userService) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<ActionResult<UserDetailsDTO>> GetUserMeAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            return await GetUserDetailsDTO(userId);
        }

        [HttpGet("{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDetailsDTO>> GetUserAsync([FromRoute] string UserId)
        {
            return await GetUserDetailsDTO(UserId);
        }

        private async Task<ActionResult<UserDetailsDTO>> GetUserDetailsDTO(string UserId)
        {
            var user = await userService.GetUserAsync(new GetUserCommand(new Guid(UserId)));
            return Ok(new UserDetailsDTO(
                RowVersion: user.Details.RowVersion,
                Email: user.Username,
                FirstName: user.Details.FirstName,
                LastName: user.Details.LastName,
                Country: user.Details.Country,
                City: user.Details.City,
                Address: user.Details.Address
            ));
        }

        [HttpPatch("me")]
        public async Task<ActionResult> PatchUserAsync(UserDetailsDTO userDetailsDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");
            return await UpdateUserDetails(userDetailsDTO, userId);
        }

        [HttpPatch("{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PatchUserAsync(UserDetailsDTO userDetailsDTO, [FromRoute] string UserId)
        {
            return await UpdateUserDetails(userDetailsDTO, UserId);
        }

        private async Task<ActionResult> UpdateUserDetails(UserDetailsDTO userDetailsDTO, string userId)
        {
            try
            {
                await userService.UpdateUserDetailsAsync(new UpdateUserDetailsCommand(
                    RowVersion: userDetailsDTO.RowVersion,
                    ExternalUserId: new Guid(userId),
                    FirstName: userDetailsDTO.FirstName,
                    LastName: userDetailsDTO.LastName,
                    Country: userDetailsDTO.Country,
                    City: userDetailsDTO.City,
                    Address: userDetailsDTO.Address
                    ));
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
            var users = await userService.GetUsersAsync(page, limit);
            var hasNextPage = users.Count > limit;
            var usersDTO = users.Take(limit).ToList();
            return Ok(new
            {
                items = usersDTO,
                hasNextPage = hasNextPage
            });
        }
    }
}