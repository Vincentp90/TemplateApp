using DataAccess.AppListings;
using DataAccess.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static WishlistApi.DTOs.AuthDTOs;

namespace WishlistApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IUserDA _userDA;
        private readonly IConfiguration _config;

        public AuthController(ILogger<AuthController> logger, IConfiguration config, IUserDA userDA)
        {
            _logger = logger;
            _userDA = userDA;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterRequest request)
        {
            if (!await _userDA.IsUsernameAvailableAsync(request.Username))
                return BadRequest("Username already taken");

            await _userDA.AddUserAsync(request.Username, request.Password);            
            return Ok();
        }

        // Could be improved by returning short lived token (<5 mins) and then other call returns bearer token, after doing extra checks (MFA, device, IP, risk scoring)
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userDA.LoginUserAsync(request.Username, request.Password);
            if(user == null)
                return Unauthorized();

            var token = CreateToken(user);
            Response.Cookies.Append("auth_token", token, new CookieOptions
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

        [HttpGet("check")]
        public async Task<ActionResult<bool>> CheckUsernameAvailableAsync([FromQuery] string username)
        {
            return Ok(await _userDA.IsUsernameAvailableAsync(username));
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new { username = User.FindFirstValue(ClaimTypes.Name) });
        }

        private string CreateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UUID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
