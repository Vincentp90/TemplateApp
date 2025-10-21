using DataAccess.AppListings;
using DataAccess.Users;
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
        private readonly UserDA _userDA;
        private readonly IConfiguration _config;

        public AuthController(ILogger<AuthController> logger, IConfiguration config, UserDA userDA)
        {
            _logger = logger;
            _userDA = userDA;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterRequest request)
        {
            if (!await _userDA.IsUsernameAvailable(request.Username))
                return BadRequest("Username already taken");

            await _userDA.AddUser(request.Username, request.Password);            
            return Ok();
        }

        // Could be improved by returning short lived token (<5 mins) and then other call returns bearer token, after doing extra checks (MFA, device, IP, risk scoring)
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _userDA.LoginUser(request.Username, request.Password);
            if(user == null)
                return Unauthorized();

            var token = CreateToken(user);
            return Ok(new AuthResponse(token));
        }

        [HttpGet("check")]
        public async Task<ActionResult<bool>> CheckUsernameAvailable([FromQuery] string username)
        {
            return Ok(await _userDA.IsUsernameAvailable(username));
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
