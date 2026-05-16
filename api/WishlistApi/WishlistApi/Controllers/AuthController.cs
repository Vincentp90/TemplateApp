using Application;
using Application.Commands;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using WishlistApi.Helpers;
using static WishlistApi.DTOs.AuthDTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController(IAuthService authService, IJwtTokenGenerator jwtTokenGenerator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterRequest request)
        {
            try
            {
                await authService.AddUserAsync(new RegisterUserCommand(request.Username, request.Password));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }

        // Could be improved by returning short lived token (<5 mins) and then other call returns bearer token, after doing extra checks (MFA, device, IP, risk scoring)
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await authService.LoginAsync(new LoginCommand(request.Username, request.Password));

            if (user == null)
                return Unauthorized();

            Response.Cookies.Append("auth_token", jwtTokenGenerator.Generate(user), new CookieOptions
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
                username = User.FindFirstValue(ClaimTypes.Name),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}
