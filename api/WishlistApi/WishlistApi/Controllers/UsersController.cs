using Application;
using Application.Commands;
using Application.Contracts;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UsersController(IUserService userService) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<ActionResult<UserDetailsDto>> GetUserMeAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");

            return await GetUserDetailsDto(userId);
        }

        [HttpGet("{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDetailsDto>> GetUserAsync([FromRoute] string UserId)
        {
            return await GetUserDetailsDto(UserId);
        }

        private async Task<ActionResult<UserDetailsDto>> GetUserDetailsDto(string UserId)
        {
            var user = await userService.GetUserAsync(new GetUserCommand(new Guid(UserId)));
            return Ok(new UserDetailsDto(
                RowVersion: user.Details.RowVersion,
                Email: user.Username,
                FirstName: user.Details.Name.FirstName,
                LastName: user.Details.Name.LastName,
                Country: user.Details.Location.Country,
                City: user.Details.Location.City,
                Address: user.Details.Location.Street
            ));
        }

        [HttpPatch("me")]
        public async Task<ActionResult> PatchUserAsync(UserDetailsDto userDetailsDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return StatusCode(StatusCodes.Status500InternalServerError, "Authenticated user has no ID claim");
            return await UpdateUserDetails(userDetailsDto, userId);
        }

        [HttpPatch("{UserId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PatchUserAsync(UserDetailsDto userDetailsDto, [FromRoute] string UserId)
        {
            return await UpdateUserDetails(userDetailsDto, UserId);
        }

        private async Task<ActionResult> UpdateUserDetails(UserDetailsDto userDetailsDto, string userId)
        {
            try
            {
                await userService.UpdateUserDetailsAsync(new UpdateUserDetailsCommand(
                    RowVersion: userDetailsDto.RowVersion,
                    ExternalUserId: new Guid(userId),
                    Name: new FullName(userDetailsDto.FirstName, userDetailsDto.LastName),
                    Location: new Address(userDetailsDto.Country, userDetailsDto.City, userDetailsDto.Address)
                    ));
                return Ok();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status409Conflict, "Concurrency conflict");
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
