using Application.Commands;
using DataAccess.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WishlistApi.Helpers
{
    // Would be more DDD like to put this in infra layer but I think this more pragmatic approach is better
    // Only this project needs the JWT nuget package this way

    public interface IJwtTokenGenerator
    {
        string Generate(LoginResult loginResult);
    }

    public class JwtTokenGenerator(IConfiguration config) : IJwtTokenGenerator
    {
        public string Generate(LoginResult loginResult)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
                new Claim(ClaimTypes.Name, loginResult.Username),
                new Claim(ClaimTypes.Role, loginResult.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
