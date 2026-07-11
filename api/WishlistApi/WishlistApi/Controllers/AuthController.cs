using Application.Contracts;
using Application.UseCases.Auth;
using Application.UseCases.Auth.Requests;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WishlistApi.Helpers;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(
        IRegisterUserUseCase registerUserUseCase,
        ILoginUserUseCase loginUserUseCase,
        IJwtTokenGenerator jwtTokenGenerator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterRequest request)
        {
            try
            {
                await registerUserUseCase.ExecuteAsync(new RegisterUserRequest(request.Username, request.Password));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }

        // Could be improved by returning short lived token (<5 mins) and then other call returns bearer token, after doing extra checks (MFA, device, IP, risk scoring)
        [HttpPost("login")]
        public async Task<ActionResult> LoginAsync(LoginRequest request)
        {
            var loginResult = await loginUserUseCase.ExecuteAsync(new LoginUserRequest(request.Username, request.Password));

            if (loginResult == null)
                return Unauthorized();

            Response.Cookies.Append("auth_token", jwtTokenGenerator.Generate(loginResult), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Ok();
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("auth_token");
            return Ok();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new {
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                username = User.FindFirstValue(ClaimTypes.Name),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}
