using Application.Contracts;
using Application.UseCases.User;
using Application.UseCases.User.Requests;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UsersController(
        IGetUserProfileUseCase getUserProfileUseCase,
        IUpdateUserProfileUseCase updateUserProfileUseCase,
        IGetPaginatedUsersUseCase getPaginatedUsersUseCase) : ControllerBase
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
            var user = await getUserProfileUseCase.ExecuteAsync(new GetUserProfileRequest(new Guid(UserId)));
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
                await updateUserProfileUseCase.ExecuteAsync(new UpdateUserProfileRequest(
                    ExternalUserId: new Guid(userId),
                    RowVersion: userDetailsDto.RowVersion,
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
            var users = await getPaginatedUsersUseCase.ExecuteAsync(new GetPaginatedUsersRequest(page, limit));
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
